using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Integration.Dtos
{
    public sealed record SkuInfo(
     Guid Id,
     Guid ProductId,
     string Name,
     int Price,
     int Discount,
     string? ImageUrl,
     bool InStock,
     int AvailableQuantity,                 
     IReadOnlyList<CharacteristicValue> Characteristics);
}
