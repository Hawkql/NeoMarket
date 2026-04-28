using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Infrastructure.Persistence.Outbox
{
    public class OutboxMessage
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = null!;
        public string Payload { get; set; } = null!;
        public DateTime OccurredOnUtc {  get; set; }
        public DateTime? ProcessedOnUtc { get; set; }
        public string? Error { get; set; }
        public int RetryCount { get; set; }
    }
}
