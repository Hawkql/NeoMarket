using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.Skus.Dtos
{
    public sealed record SkuImageInputDto(string Url, int Ordering);
    public sealed record SkuCharacteristicInputDto(string Name, string Value);
}
