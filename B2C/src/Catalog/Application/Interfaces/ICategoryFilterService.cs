using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Interfaces
{
    public interface ICategoryFilterService
    {
        /// <summary>
        /// Список доступных фильтров для страницы категории.
        /// Используется для построения UI фильтрации (sidebar).
        /// </summary>
        Task<FiltersListDto> GetFiltersAsync(Guid categoryId, CancellationToken ct = default);

        /// <summary>
        /// Фасеты с подсчётом товаров при текущих активных фильтрах.
        /// Вызывается при каждом изменении фильтров на клиенте.
        /// </summary>
        Task<FacetsDto> GetFacetsAsync(
            Guid categoryId,
            Dictionary<string, string>? activeFilters,
            CancellationToken ct = default);
    }
}
