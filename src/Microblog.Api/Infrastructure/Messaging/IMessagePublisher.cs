namespace Microblog.Api.Infrastructure.Messaging;

/// <summary>Publishes domain events to the configured message bus (Azure Service Bus).</summary>
public interface IMessagePublisher
{
    /// <summary>Publishes <paramref name="message"/> to <paramref name="topic"/>.</summary>
    Task PublishAsync<T>(string topic, T message, CancellationToken ct = default) where T : class;
}
