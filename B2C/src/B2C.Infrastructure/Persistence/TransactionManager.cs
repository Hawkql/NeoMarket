using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Common.Interface;

using MediatR;
using Microsoft.EntityFrameworkCore.Storage;

namespace B2C.Infrastructure.Persistence
{
    public sealed class TransactionManager : ITransactionManager
    {
        private readonly B2CDbContext _db;
        private readonly IPublisher _publisher;
        private IDbContextTransaction? _transaction;

        public TransactionManager(B2CDbContext db, IPublisher publisher)
        {
            _db = db;
            _publisher = publisher;
        }

        public async Task BeginAsync(CancellationToken ct)
        {
            if (_transaction is not null) return;
            _transaction = await _db.Database.BeginTransactionAsync(ct);
        }

        public async Task CommitAsync(CancellationToken ct)
        {
            if (_transaction is null) return;

            // 1. Собрать domain events ДО SaveChanges (потом ChangeTracker может измениться).
            var domainEvents = _db.ExtractDomainEvents();

            // 2. Записать доменные изменения.
            await _db.SaveChangesAsync(ct);

            // 3. Зафиксировать транзакцию.
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;

            // 4. Опубликовать domain events ПОСЛЕ commit (внутренняя доставка через MediatR).
            //    Handler'ы (OrderDeliveredDomainEventHandler → fulfill) сработают здесь.
            foreach (var domainEvent in domainEvents)
                await _publisher.Publish(domainEvent, ct);
        }

        public async Task RollbackAsync(CancellationToken ct)
        {
            if (_transaction is null) return;
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
            // События НЕ публикуем — транзакция откачена, изменений нет.
        }
    }
}
