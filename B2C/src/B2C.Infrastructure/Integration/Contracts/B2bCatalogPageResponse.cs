using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Infrastructure.Integration.Contracts
{
    public sealed class B2bCatalogPageResponse
    {
        public List<B2bProductSummaryResponse> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Limit { get; set; }
        public int Offset { get; set; }
    }
}
