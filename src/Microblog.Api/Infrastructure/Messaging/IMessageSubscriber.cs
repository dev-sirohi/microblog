namespace Microblog.Api.Infrastructure.Messaging
{
    public interface IMessageSubscriber
    {
        void Subscribe(string topic);
        void Subscribe(IEnumerable<string> topicList);
        Task SubscribeAsync<T>(Func<T, Task> onMessage, CancellationToken ct = default) where T : class;
    }
}
