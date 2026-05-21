using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Infrastructure.Outbox
{
    public sealed class OutboxMessage
    {
        // Public set'теры — это инфраструктурная запись, не Domain.
        // EF будет читать/писать напрямую. Без инкапсуляции методами.

        public Guid Id { get; set; }

        /// <summary>
        /// Тип события для маппинга и фильтрации.
        /// Пример: "product.created.v1", "sku.out_of_stock.v1".
        /// Версия в конце — обязательно (Integration Events versioning).
        /// </summary>
        public string EventType { get; set; } = null!;

        /// <summary>JSON Integration Event целиком.</summary>
        public string Payload { get; set; } = null!;

        /// <summary>
        /// Куда отправлять. Определяет, какой IIntegrationEventDispatcher
        /// будет обрабатывать это сообщение.
        /// 
        /// Значения: "moderation", "b2c", "kafka".
        /// </summary>
        public string Destination { get; set; } = null!;

        /// <summary>Id агрегата для отладки.</summary>
        public Guid? AggregateId { get; set; }

        /// <summary>Тип агрегата ("Product", "Sku", ...) для отладки и партиционирования.</summary>
        public string? AggregateType { get; set; }

        /// <summary>Когда событие случилось в Domain (из Domain Event).</summary>
        public DateTime OccurredOnUtc { get; set; }

        /// <summary>Когда фоновый процессор успешно отправил событие. null = не обработано.</summary>
        public DateTime? ProcessedOnUtc { get; set; }

        /// <summary>Сообщение об ошибке последней попытки.</summary>
        public string? Error { get; set; }

        /// <summary>Количество попыток отправки.</summary>
        public int RetryCount { get; set; }
    }
}
