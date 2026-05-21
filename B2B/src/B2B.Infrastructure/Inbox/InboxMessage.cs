using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Infrastructure.Inbox
{
    public sealed class InboxMessage
    {
        /// <summary>Идемпотентный ключ от отправителя. Primary Key.</summary>
        public Guid IdempotencyKey { get; set; }

        /// <summary>Тип события для отладки и логов.</summary>
        public string MessageType { get; set; } = null!;

        /// <summary>Откуда пришло событие ("moderation", "b2c", ...).</summary>
        public string Source { get; set; } = null!;

        /// <summary>JSON payload — для аудита и debugging.</summary>
        public string Payload { get; set; } = null!;

        public DateTime ReceivedOnUtc { get; set; }
        public DateTime? ProcessedOnUtc { get; set; }
        public string? Error { get; set; }
    }
}
