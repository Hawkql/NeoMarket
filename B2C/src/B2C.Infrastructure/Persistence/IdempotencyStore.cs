using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Abstractions;
using B2C.Infrastructure.Inbox;

using Microsoft.EntityFrameworkCore;

namespace B2C.Infrastructure.Persistence
{
    public sealed class IdempotencyStore : IIdempotencyStore
    {
        private readonly B2CDbContext _db;

        public IdempotencyStore(B2CDbContext db) => _db = db;

        public async Task<bool> ExistsAsync(Guid idempotencyKey, CancellationToken ct)
            => await _db.InboxMessages
                .AsNoTracking()
                .AnyAsync(m => m.IdempotencyKey == idempotencyKey, ct);

        public void Register(Guid idempotencyKey, string messageType, string source, string payload)
        {
            _db.InboxMessages.Add(new InboxMessage
            {
                IdempotencyKey = idempotencyKey,
                MessageType = messageType,
                Source = source,
                Payload = payload,
                ReceivedOnUtc = DateTime.UtcNow,
                ProcessedOnUtc = DateTime.UtcNow,
            });
        }
    }
}
