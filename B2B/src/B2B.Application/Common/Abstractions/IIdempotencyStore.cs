using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.Common.Abstractions
{
    /// <summary>
    /// Хранилище идемпотентных ключей (Inbox-паттерн). Реализация в Infrastructure
    /// поверх таблицы inbox_messages.
    /// </summary>
    public interface IIdempotencyStore
    {
        /// <summary>Уже обрабатывали этот ключ?</summary>
        Task<bool> ExistsAsync(Guid idempotencyKey, CancellationToken ct);

        /// <summary>
        /// Зарегистрировать ключ как обработанный. Добавляет запись в текущую
        /// транзакцию (SaveChanges вызывается оркестратором). При дубле PK
        /// сохранение упадёт — это защита от race.
        /// </summary>
        void Register(Guid idempotencyKey, string messageType, string source, string payload);
    }
}
