using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Abstractions;
using B2B.Infrastructure.Inbox;
using Microsoft.EntityFrameworkCore;

namespace B2B.Infrastructure.Persistence
{

    public sealed class IdempotencyStore : IIdempotencyStore
    {
        private readonly B2BDbContext _dbContext;

        public IdempotencyStore(B2BDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> ExistsAsync(Guid idempotencyKey, CancellationToken ct)
        {
            return await _dbContext.InboxMessages
                .AsNoTracking()
                .AnyAsync(m => m.IdempotencyKey == idempotencyKey, ct);
        }

        public void Register(
            Guid idempotencyKey, string messageType, string source, string payload)
        {
            _dbContext.InboxMessages.Add(new InboxMessage
            {
                IdempotencyKey = idempotencyKey,
                MessageType = messageType,
                Source = source,
                Payload = payload,
                ReceivedOnUtc = DateTime.UtcNow,
                ProcessedOnUtc = DateTime.UtcNow
            });
        }
    }
}
