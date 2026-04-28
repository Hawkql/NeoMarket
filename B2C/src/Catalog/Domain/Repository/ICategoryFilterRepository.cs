using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Repository
{
    public interface ICategoryFilterRepository
    {
        Task<List<CategoryFilter>> GetByCategoryIdAsync(Guid categoryId, CancellationToken ct = default);

        /// <summary>
        /// Фасеты — GROUP BY характеристик с подсчётом товаров при активных фильтрах.
        /// Возвращает кортеж: (имя_фильтра, значение, количество).
        /// </summary>
        Task<List<(string FilterName, string Value, int Count)>> GetFacetsAsync(
            Guid categoryId,
            Dictionary<string, string>? activeFilters,
            CancellationToken ct = default);
    }
}
