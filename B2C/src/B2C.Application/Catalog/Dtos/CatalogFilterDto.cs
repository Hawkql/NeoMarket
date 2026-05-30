using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.Catalog.Dtos
{
    /// <summary>Определение фильтра с возможными значениями и количеством товаров.</summary>
    public sealed record CatalogFilterDto(
        string Slug,
        string Name,
        IReadOnlyList<CatalogFilterValueDto> Values);

    public sealed record CatalogFilterValueDto(string Value, int Count);
}
