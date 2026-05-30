using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Infrastructure.Integration.Contracts
{
    // ===== Request =====
    public sealed class B2bReserveRequest
    {
        public Guid IdempotencyKey { get; set; }
        public List<B2bReserveLine> Items { get; set; } = new();
    }

    public sealed class B2bReserveLine
    {
        public Guid SkuId { get; set; }
        public int Quantity { get; set; }
    }

    public sealed class B2bFulfillRequest
    {
        public Guid OrderId { get; set; }
        public List<B2bReserveLine> Items { get; set; } = new();
    }

    // ===== Response =====
    public sealed class B2bReserveResponse
    {
        public bool Success { get; set; }
        public List<B2bReserveFailedItem> FailedItems { get; set; } = new();
    }

    public sealed class B2bReserveFailedItem
    {
        public Guid SkuId { get; set; }
        public int Requested { get; set; }
        public int Available { get; set; }
        public string Reason { get; set; } = null!;   // строковый код от B2B
    }
}
