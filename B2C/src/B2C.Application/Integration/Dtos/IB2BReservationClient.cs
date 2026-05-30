using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Integration.Dtos
{
    /// <summary>
    /// Порт для write-операций к B2B инвентарю: резервирование, освобождение, fulfill.
    /// All-or-nothing semantics — гарантирует B2B на своей стороне.
    /// 
    /// Idempotency: каждый вызов содержит idempotencyKey, чтобы при retry не задвоить эффект.
    /// </summary>
    public interface IB2BReservationClient
    {
        /// <summary>
        /// US-ORD-01: резервирование при checkout. Либо все items зарезервированы,
        /// либо ни один (B2B сам делает транзакцию).
        /// </summary>
        Task<ReserveResult> ReserveAsync(
            Guid idempotencyKey,
            IReadOnlyList<ReserveLine> items,
            CancellationToken ct);

        /// <summary>
        /// US-ORD-03: освобождение резерва при отмене заказа.
        /// Возвращает true при успехе. Возвращает false / бросает HttpRequestException —
        /// заказ уходит в CANCEL_PENDING для background-retry.
        /// </summary>
        Task<bool> UnreserveAsync(
            Guid idempotencyKey,
            IReadOnlyList<ReserveLine> items,
            CancellationToken ct);

        /// <summary>
        /// US-ORD-05: финальное списание резерва при DELIVERED.
        /// Идемпотентно на стороне B2B: повторный вызов с тем же orderId = 200 без изменений.
        /// </summary>
        Task<bool> FulfillAsync(
            Guid orderId,
            IReadOnlyList<ReserveLine> items,
            CancellationToken ct);
    }
}
