using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Integration.Dtos;

namespace B2C.Tests.Infrastructure
{
    public sealed class FakeB2BReservationClient : IB2BReservationClient
    {
        public ReserveBehavior ReserveBehavior { get; set; } = ReserveBehavior.AlwaysSucceed;
        public bool UnreserveAlwaysFails { get; set; }
        public bool FulfillAlwaysFails { get; set; }

        public List<ReserveCall> ReserveCalls { get; } = new();
        public List<Guid> UnreserveCalls { get; } = new();
        public List<Guid> FulfillCalls { get; } = new();

        // Идемпотентный кэш ответов reserve по ключу.
        private readonly Dictionary<Guid, ReserveResult> _reserveResults = new();

        public Task<ReserveResult> ReserveAsync(
            Guid idempotencyKey, IReadOnlyList<ReserveLine> items, CancellationToken ct)
        {
            ReserveCalls.Add(new ReserveCall(idempotencyKey, items));

            if (_reserveResults.TryGetValue(idempotencyKey, out var cached))
                return Task.FromResult(cached);

            var result = ReserveBehavior switch
            {
                ReserveBehavior.AlwaysSucceed => new ReserveResult(true, Array.Empty<ReserveFailedItem>()),
                ReserveBehavior.AlwaysFail => new ReserveResult(false, BuildFailedItems(items)),
                _ => new ReserveResult(true, Array.Empty<ReserveFailedItem>()),
            };

            _reserveResults[idempotencyKey] = result;
            return Task.FromResult(result);
        }

        public Task<bool> UnreserveAsync(
            Guid idempotencyKey, IReadOnlyList<ReserveLine> items, CancellationToken ct)
        {
            UnreserveCalls.Add(idempotencyKey);
            return Task.FromResult(!UnreserveAlwaysFails);
        }

        public Task<bool> FulfillAsync(
            Guid orderId, IReadOnlyList<ReserveLine> items, CancellationToken ct)
        {
            FulfillCalls.Add(orderId);
            return Task.FromResult(!FulfillAlwaysFails);
        }

        private static IReadOnlyList<ReserveFailedItem> BuildFailedItems(IReadOnlyList<ReserveLine> items)
        {
            var failed = new List<ReserveFailedItem>(items.Count);
            foreach (var item in items)
                failed.Add(new ReserveFailedItem(item.SkuId, item.Quantity, 0, ReserveFailReason.OutOfStock));
            return failed;
        }
    }

    public enum ReserveBehavior
    {
        AlwaysSucceed,
        AlwaysFail,
    }

    public sealed record ReserveCall(Guid IdempotencyKey, IReadOnlyList<ReserveLine> Items);
}
