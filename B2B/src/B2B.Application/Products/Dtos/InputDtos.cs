using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.Products.Dtos
{
    /// <summary>Изображение во входящем запросе создания товара.</summary>
    public sealed record ImageInputDto(string Url, int Ordering);
    /// <summary>Характеристика во входящем запросе.</summary>
    public sealed record CharacteristicInputDto(string Name, string Value);
}
