namespace Microblog.Api.Infrastructure.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(string topic, T message, CancellationToken ct = default) where T : class;
}
