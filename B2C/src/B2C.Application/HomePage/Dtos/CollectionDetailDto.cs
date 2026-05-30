using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Catalog.Dtos;

namespace B2C.Application.HomePage.Dtos
{
    /// <summary>
    /// Детальная подборка с обогащёнными товарами.
    /// Переиспользуем CatalogProductCardDto — это тот же формат карточки, что в каталоге.
    /// </summary>
    public sealed record CollectionDetailDto(
       Guid Id,
       string Slug,
       string Title,
       string? Description,
       string? CoverImageUrl,
       IReadOnlyList<CatalogProductCardDto> Products,
       IReadOnlyList<Guid> UnavailableIds);
}
