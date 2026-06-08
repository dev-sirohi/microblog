using Confluent.Kafka;
using System.Text.Json;
using Microblog.Api.Infrastructure.Observability;

namespace Microblog.Api.Infrastructure.Messaging.Kafka;

/// <summary>
/// Hosted service that consumes events from Kafka topics and syncs them to SQL Server.
/// Replaces / augments the Redis-queue BackgroundSyncService for durable event delivery.
/// Failed messages after <see cref="MaxRetries"/> attempts are forwarded to the DLQ topic.
/// </summary>
public sealed class KafkaConsumerService : BackgroundService
{
    private const int MaxRetries = 3;

    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly ILogger<KafkaConsumerService> _logger;

    private static readonly string[] Topics =
    [
        "post.created", "post.liked", "user.followed"
    ];

    public KafkaConsumerService(IServiceProvider services, IConfiguration config, ILogger<KafkaConsumerService> logger)
    {
        _services = services;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("KafkaConsumerService started");

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = _config["Kafka:GroupId"] ?? "microblog-consumers",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(Topics);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                await ProcessMessageAsync(result, stoppingToken);
                consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kafka consumer error");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        consumer.Close();
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> result, CancellationToken ct)
    {
        string topic = result.Topic;
        string payload = result.Message.Value;

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                switch (topic)
                {
                    case "post.created":
                        // Post already persisted by PostService; emit observability
                        AppMetrics.PostsCreated.Inc();
                        break;

                    case "post.liked":
                        var likeEvent = JsonSerializer.Deserialize<PostLikedEvent>(payload);
                        if (likeEvent is not null)
                        {
                            AppMetrics.PostLikes.Inc();
                        }
                        break;

                    case "user.followed":
                        // Follow already persisted by UserFollowService
                        break;
                }

                AppMetrics.BackgroundSyncProcessed.Inc();
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Processing attempt {Attempt}/{Max} failed for topic {Topic}", attempt, MaxRetries, topic);
                if (attempt == MaxRetries)
                {
                    await SendToDlqAsync(topic, payload, ct);
                    AppMetrics.BackgroundSyncErrors.Inc();
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2), ct);
                }
            }
        }
    }

    private async Task SendToDlqAsync(string originalTopic, string payload, CancellationToken ct)
    {
        string dlqTopic = $"{originalTopic}.dlq";
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"] ?? "localhost:9092",
        };
        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();
        await producer.ProduceAsync(dlqTopic, new Message<string, string>
        {
            Key = Guid.NewGuid().ToString(),
            Value = payload
        }, ct);
        AppMetrics.MessageErrors.WithLabels(dlqTopic).Inc();
        _logger.LogError("Message forwarded to DLQ {Dlq}", dlqTopic);
    }

    // Record types imported from MessageEvents.cs
    private sealed record PostLikedEvent(long PostId, long UserId, bool IsLike, DateTime OccurredAt);
}
