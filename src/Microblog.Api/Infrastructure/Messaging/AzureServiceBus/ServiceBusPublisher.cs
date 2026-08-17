using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace Microblog.Api.Infrastructure.Messaging.AzureServiceBus;

public sealed class ServiceBusPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;
    private readonly ILogger<ServiceBusPublisher> _logger;
    private readonly string _queueName;

    public ServiceBusPublisher(IConfiguration config, ILogger<ServiceBusPublisher> logger)
    {
        _logger = logger;
        string connectionString = config["Azure:ServiceBusConnectionString"]
            ?? throw new InvalidOperationException("Azure:ServiceBusConnectionString is required when using azure-service-bus messaging");
        _queueName = config["Azure:ServiceBusQueueName"] ?? "microblog-events";
        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(_queueName);
    }

    public async Task PublishAsync<T>(string topic, T message, CancellationToken ct = default) where T : class
    {
        string payload = JsonSerializer.Serialize(new { Topic = topic, Payload = message });
        var sbMessage = new ServiceBusMessage(payload)
        {
            Subject = topic,
            MessageId = Guid.NewGuid().ToString()
        };

        try
        {
            await _sender.SendMessageAsync(sbMessage, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish Service Bus message for topic {Topic}", topic);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
