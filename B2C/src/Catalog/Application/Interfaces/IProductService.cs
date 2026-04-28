using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Interfaces
{
    public interface IProductService
    {
        /// <summary>Полная карточка товара. Выбрасывает NotFoundException если товар не найден или не Moderated.</summary>
        Task<ProductDto> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>Листинг товаров с фильтрами, поиском и пагинацией.</summary>
        Task<ProductListDto> ListAsync(
            Guid? categoryId,
            string? search,
            Dictionary<string, string>? filters,
            string? sort,
            int limit,
            int offset,
            CancellationToken ct = default);

        /// <summary>Похожие товары из той же категории.</summary>
        Task<ProductListDto> GetSimilarAsync(
            Guid productId,
            Guid categoryId,
            int limit,
            int offset,
            CancellationToken ct = default);
    }
}
