using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Infrastructure.Integration.Contracts
{
    public sealed class B2bProductDetailResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public Guid CategoryId { get; set; }
        public List<string> ImageUrls { get; set; } = new();
        public List<B2bSkuResponse> Skus { get; set; } = new();
        public List<B2bCharacteristicResponse> Characteristics { get; set; } = new();
        public double? Rating { get; set; }
        public int? ReviewsCount { get; set; }
    }
}
