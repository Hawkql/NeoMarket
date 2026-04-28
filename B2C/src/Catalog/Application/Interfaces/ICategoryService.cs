using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Interfaces
{
    public interface ICategoryService
    {
        /// <summary>Дерево категорий для навигации и левого меню.</summary>
        Task<CategoryTreeDto> GetTreeAsync(CancellationToken ct = default);

        /// <summary>Детальная информация о категории (SEO, мета-теги, родитель).</summary>
        Task<CategoryDetailDto> GetDetailAsync(
            Guid id,
            bool includeProductCount,
            string lang,
            CancellationToken ct = default);
    }
}
