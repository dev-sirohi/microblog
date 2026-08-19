using Confluent.Kafka;

namespace Microblog.Api.Infrastructure.Messaging.Kafka
{
    public class KafkaProducer : IMessagePublisher
    {
        private readonly IConfiguration _configuration;
        private readonly IProducer<string, string> _producer;

        public KafkaProducer(IConfiguration configuration)
        {
            _configuration = configuration;
            _producer = new ProducerBuilder<string, string>(new ProducerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"],
            }).Build();
        }

        public async Task PublishAsync<T>(string topic, T message, CancellationToken ct = default) where T : class
        {
            var kafkaMessage = CommonUtils.TransformTo<Message<string, string>>(message);

            try
            {
                var result = await _producer.ProduceAsync(topic, kafkaMessage, ct);
            }
            catch (KafkaException kEx)
            {
                // TODO
            }

            int remainingMessages = _producer.Flush(TimeSpan.FromSeconds(10));
        }
    }
}
