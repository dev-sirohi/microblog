using Confluent.Kafka;
using System.Text.Json;
using Microblog.Api.Infrastructure.Observability;

namespace Microblog.Api.Infrastructure.Messaging.Kafka;

/// <summary>Publishes domain events to Kafka topics using fire-and-forget delivery with error logging.</summary>
public sealed class KafkaPublisher : IMessagePublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaPublisher> _logger;

    public KafkaPublisher(IConfiguration config, ILogger<KafkaPublisher> logger)
    {
        _logger = logger;
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092",
            Acks = Acks.Leader,
            MessageTimeoutMs = 10_000,
        };
        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    public async Task PublishAsync<T>(string topic, T message, CancellationToken ct = default) where T : class
    {
        string payload = JsonSerializer.Serialize(message);
        try
        {
            var result = await _producer.ProduceAsync(topic,
                new Message<string, string> { Key = Guid.NewGuid().ToString(), Value = payload }, ct);

            AppMetrics.MessagesPublished.WithLabels(topic).Inc();
            _logger.LogDebug("Published message to {Topic} offset {Offset}", topic, result.Offset);
        }
        catch (Exception ex)
        {
            AppMetrics.MessageErrors.WithLabels(topic).Inc();
            _logger.LogError(ex, "Failed to publish message to topic {Topic}", topic);
            throw;
        }
    }

    public void Dispose() => _producer.Dispose();
}
