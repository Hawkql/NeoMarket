using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace B2B.Infrastructure.Messaging
{
    public interface IkafkaProducer
    {
        Task ProduceAsync(string topic, string key, string payload, CancellationToken ct);
    }
    public class KafkaProducer : IkafkaProducer ,IDisposable
    {
        private readonly IProducer<string, string> _producer;

        public KafkaProducer(string bootstrapServers)
        {
            var confic = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true,
                CompressionType = CompressionType.Snappy,
                LingerMs = 5
            };
            _producer = new ProducerBuilder<string, string>(confic).Build();
            
        }
        public async Task ProduceAsync(string topic, string key, string payload, CancellationToken ct)
        {
            await _producer.ProduceAsync(topic,new Message<string, string>{ Key=key,Value=payload},ct); 
        }
        public void Dispose()
        {
            _producer.Flush(TimeSpan.FromSeconds(10));
            _producer.Dispose();

        }

       
    }
}
