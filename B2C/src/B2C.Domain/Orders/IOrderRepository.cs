using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Domain.Orders
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(Guid id, CancellationToken ct);

        /// <summary>
        /// Получить заказ ТОЛЬКО если он принадлежит указанному покупателю.
        /// Используется на всех buyer-facing endpoints (GET /orders/{id}, POST /cancel).
        /// 
        /// Возвращает null, если заказ не существует ИЛИ принадлежит другому покупателю.
        /// Это согласовано с правилом IDOR: всегда 404, никогда 403
        /// (b2c-orders-flows: other_user_order_returns_404_not_403).
        /// Реализация фильтрует одним WHERE — снаружи различать "не найдено" и "чужой" невозможно.
        /// </summary>
        Task<Order?> GetByIdForBuyerAsync(Guid orderId, Guid buyerId, CancellationToken ct);

        /// <summary>
        /// Idempotency check: найти заказ по ключу для конкретного покупателя.
        /// Если найден — возвращаем существующий (US-ORD-01: idempotency_returns_existing_order).
        /// </summary>
        Task<Order?> GetByIdempotencyKeyAsync(Guid buyerId, IdempotencyKey key, CancellationToken ct);

        /// <summary>
        /// Список заказов покупателя с пагинацией и опциональной фильтрацией по статусу.
        /// Сортировка — created_at DESC (новые сверху).
        /// </summary>
        Task<(IReadOnlyList<Order> Items, int TotalCount)> ListByBuyerAsync(
            Guid buyerId,
            OrderStatus? statusFilter,
            int limit,
            int offset,
            CancellationToken ct);

        /// <summary>
        /// Заказы в статусе CancelPending, у которых последняя попытка unreserve была
        /// раньше указанного времени — для background-retry job (US-ORD-03 ADR).
        /// </summary>
        Task<IReadOnlyList<Order>> ListPendingCancelOlderThanAsync(
            DateTime threshold, int limit, CancellationToken ct);

        /// <summary>
        /// Заказы в статусе Delivered, у которых fulfill ещё не завершён — для background-retry
        /// (US-ORD-05: fulfill_failure_retried_asynchronously).
        /// </summary>
        Task<IReadOnlyList<Order>> ListPendingFulfillAsync(int limit, CancellationToken ct);

        Task AddAsync(Order order, CancellationToken ct);
        void Update(Order order);
    }
}
