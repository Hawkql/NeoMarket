using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.Products.Dtos
{
    public sealed record SkuDto(
        Guid Id,
        Guid ProductId,
        string Name,
        int Price,
        int CostPrice,
        int Discount,
        string ImageUrl,
        int ActiveQuantity,
        int ReservedQuantity,
        bool Deleted);
}
