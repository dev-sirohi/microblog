using Confluent.Kafka;
using Newtonsoft.Json;

namespace Microblog.Api.Infrastructure.Messaging.Kafka
{
    public class KafkaConsumer : IMessageSubscriber, IDisposable
    {
        private IConfiguration _configuration;
        private IConsumer<string, string> _consumer;

        public KafkaConsumer(IConfiguration configuration, string groupId)
        {
            _configuration = configuration;
            _consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"],
            }).Build();
        }

        public async Task SubscribeAsync<T>(Func<T, Task> onMessage, CancellationToken ct = default) where T : class
        {
            while (!ct.IsCancellationRequested)
            {
                var result = _consumer.Consume(ct);
                if (result.Message.Value is not null)
                {
                    var consumedMessage = JsonConvert.DeserializeObject<T>(result.Message.Value);
                    if (consumedMessage is not null)
                    {
                        await onMessage(consumedMessage);
                    }
                    _consumer.Commit(result);
                }
            }
        }

        public void Subscribe(string topic)
        {
            _consumer.Subscribe(topic);
        }

        public void Subscribe(IEnumerable<string> topicList)
        {
            _consumer.Subscribe(topicList);
        }

        public void Dispose()
        {
            _consumer.Close();
            _consumer.Dispose();
        }
    }
}
