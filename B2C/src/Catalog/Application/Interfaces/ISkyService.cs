using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Interfaces
{
    public interface ISkuService
    {
        /// <summary>Список SKU товара (краткий формат для карточки).</summary>
        Task<IReadOnlyList<SkuShortDto>> GetSkusAsync(Guid productId, CancellationToken ct = default);

        /// <summary>Конкретный SKU по productId + skuId.</summary>
        Task<SkuDto> GetSkuAsync(Guid productId, Guid skuId, CancellationToken ct = default);
    }
}
