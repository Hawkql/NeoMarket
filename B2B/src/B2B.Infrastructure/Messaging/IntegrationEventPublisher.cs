using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace B2B.Infrastructure.Messaging
{
    public interface IIntegrationEventPublisher
    {
        Task PublishAsync( 
            string topic,
            string key,
            object payload,
            string messageId,
            string eventType,
            CancellationToken ct);
    }
    public class IntegrationEventPublisher : IIntegrationEventPublisher
    {
        private readonly IkafkaProducer _producer;
        private readonly ILogger<IntegrationEventPublisher> _logger;
        private static readonly JsonSerializerOptions jsonSerializerOptions = new();

        public IntegrationEventPublisher(IkafkaProducer producer, ILogger<IntegrationEventPublisher> logger)
        {
            _producer = producer;
             _logger = logger;
        }
        public async Task PublishAsync(string topic, string key, object payload, string messageId, string eventType, CancellationToken ct)
        {
            var json = JsonSerializer.Serialize(payload, payload.GetType(), jsonSerializerOptions);

            var headers = new Headers
        {
            { "message-id", Encoding.UTF8.GetBytes(messageId) },
            { "event-type", Encoding.UTF8.GetBytes(eventType) },
            { "occurred-on", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) }
        };

            await _producer.ProduceAsync(topic, key, json, headers, ct);

            _logger.LogInformation(
                "Published {EventType} to {Topic} (key={Key}, msgId={MessageId})",
                eventType, topic, key, messageId);
        }
    }
}
