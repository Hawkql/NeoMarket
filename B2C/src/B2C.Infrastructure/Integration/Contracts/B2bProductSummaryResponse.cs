using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Infrastructure.Integration.Contracts
{
    /// <summary>
    /// Карточка товара в списках B2B. cost_price / reserved_quantity сюда не маппим —
    /// даже если B2B их вернёт, они не попадут в наш ProductSummary (ACL US-CAT-03).
    /// </summary>
    public sealed class B2bProductSummaryResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public int Price { get; set; }
        public int? OldPrice { get; set; }
        public int? Discount { get; set; }
        public bool InStock { get; set; }
        public double? Rating { get; set; }
        public int? ReviewsCount { get; set; }
    }
}
