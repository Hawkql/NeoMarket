using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2C.Application.HomePage.Dtos
{
    /// <summary>
    /// Краткие данные подборки — для списка подборок на главной.
    /// БЕЗ списка товаров: чтобы при отображении главной не делать N запросов в B2B.
    /// Товары загружаются отдельным запросом при клике на подборку.
    /// </summary>
    public sealed record CollectionSummaryDto(
        Guid Id,
        string Slug,
        string Title,
        string? Description,
        string? CoverImageUrl,
        int ProductCount);
}
