using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace B2B.Infrastructure.Messaging
{
    public interface IKafkaProducer
    {
        Task ProduceAsync(string topic, string key, string payload,Headers headers, CancellationToken ct);
    }
    public class KafkaProducer : IKafkaProducer ,IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaProducer> _logger;

        public KafkaProducer(IConfiguration config, ILogger<KafkaProducer> logger)
        {
            _logger= logger;
            var Producerconfig = new ProducerConfig
            {
                BootstrapServers = config["Kafka:BootstrapServers"]
                ?? throw new InvalidOperationException("Kafka:BootstrapServers not configured"),
                Acks = Acks.All,
                EnableIdempotence = true,
                CompressionType = CompressionType.Snappy,
                LingerMs = 5,
                MessageSendMaxRetries = 5,
                RetryBackoffMaxMs = 5,
                
            };
            _producer = new ProducerBuilder<string, string>(Producerconfig)
                .SetErrorHandler((_,e)=>
                    _logger.LogError("Kafka producer error: {Reason}", e.Reason))
                .Build();
            
        }
        public async Task ProduceAsync(string topic, string key, string payload,Headers headers, CancellationToken ct)
        {
            var message = new Message<string, string>
            {
                Key = key,
                Value = payload,
                Headers = headers

            };
            await _producer.ProduceAsync(topic,message, ct);
        }
        public void Dispose()
        {
            _producer.Flush(TimeSpan.FromSeconds(10));
            _producer.Dispose();

        }

       
    }
}
