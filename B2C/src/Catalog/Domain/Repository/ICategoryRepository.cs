using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Repository
{
    public interface ICategoryRepository
    {
        /// <summary>Все активные категории (плоский список для построения дерева)</summary>
        Task<List<Category>> GetAllFlatAsync(CancellationToken ct = default);

        Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<int> CountProductsAsync(Guid categoryId, CancellationToken ct = default);

        /// <summary>Цепочка предков от корня до указанного узла (рекурсивный CTE)</summary>
        Task<List<Category>> GetAncestorsAsync(Guid categoryId, CancellationToken ct = default);

        /// <summary>Категория товара по product_id</summary>
        Task<Guid?> GetCategoryIdByProductAsync(Guid productId, CancellationToken ct = default);
    }
}
