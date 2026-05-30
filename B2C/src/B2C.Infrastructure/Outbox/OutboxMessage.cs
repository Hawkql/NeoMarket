using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Infrastructure.Outbox
{
    public sealed class OutboxMessage
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Тип события с версией: "order.delivered.v1", "order.created.v1".
        /// Версия в конце — обязательно (Integration Events versioning).
        /// </summary>
        public string EventType { get; set; } = null!;

        /// <summary>JSON Integration Event целиком.</summary>
        public string Payload { get; set; } = null!;

        /// <summary>
        /// Куда отправлять — выбирает dispatcher. Для B2C обычно "b2b"
        /// (например, fulfill-вызовы), но если событий наружу нет — это поле просто
        /// не используется dispatcher'ами.
        /// </summary>
        public string Destination { get; set; } = null!;

        /// <summary>Id агрегата для отладки/корреляции.</summary>
        public Guid? AggregateId { get; set; }

        /// <summary>Тип агрегата ("Order", "Cart") для отладки.</summary>
        public string? AggregateType { get; set; }

        /// <summary>Когда событие случилось в Domain.</summary>
        public DateTime OccurredOnUtc { get; set; }

        /// <summary>Когда фоновый процессор успешно отправил. null = не обработано.</summary>
        public DateTime? ProcessedOnUtc { get; set; }

        /// <summary>Сообщение об ошибке последней попытки.</summary>
        public string? Error { get; set; }

        /// <summary>Количество попыток отправки.</summary>
        public int RetryCount { get; set; }
    }
}
