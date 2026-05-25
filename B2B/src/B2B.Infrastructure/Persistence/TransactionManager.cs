using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Common.Interface;
using Microsoft.EntityFrameworkCore.Storage;

namespace B2B.Infrastructure.Persistence
{
    /// <summary>
    /// Реализация ITransactionManager поверх EF Core IDbContextTransaction.
    /// Держит транзакцию на том же B2BDbContext (scoped), что и репозитории —
    /// это гарантирует, что FOR UPDATE, UPDATE и Outbox идут в одной транзакции
    /// и одном соединении.
    /// </summary>
    public sealed class TransactionManager : ITransactionManager
    {
        private readonly B2BDbContext _dbContext;
        private IDbContextTransaction? _transaction;

        public TransactionManager(B2BDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task BeginAsync(CancellationToken ct)
        {
            // Если транзакция уже открыта (вложенный вызов) — переиспользуем,
            // не открываем вторую.
            if (_transaction is not null)
                return;

            _transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        }

        public async Task CommitAsync(CancellationToken ct)
        {
            if (_transaction is null)
                return;

            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        public async Task RollbackAsync(CancellationToken ct)
        {
            if (_transaction is null)
                return;

            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
