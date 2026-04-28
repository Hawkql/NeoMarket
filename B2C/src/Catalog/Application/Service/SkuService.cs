using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Domain.Exceptions;
using Domain.Repository;
using Microsoft.Extensions.Logging;

namespace Application.Service
{
    public sealed class SkuService(
    ISkuRepository skuRepository,
    IProductRepository productRepository,
    ILogger<SkuService> logger) : ISkuService
    {
        public async Task<IReadOnlyList<SkuShortDto>> GetSkusAsync(Guid productId, CancellationToken ct = default)
        {
            // Проверяем что товар существует и виден
            var product = await productRepository.GetByIdAsync(productId, ct);
            if (product is null || !product.IsPubliclyVisible)
                throw new NotFoundException("Product", productId.ToString());

            var skus = await skuRepository.GetByProductIdAsync(productId, ct);

            logger.LogDebug("Returning {Count} SKUs for product {ProductId}", skus.Count, productId);

            return skus.Select(s => new SkuShortDto(
                s.Name,
                s.Price,
                s.Images.OrderBy(i => i.Order)
                        .Select(i => new ImageDto(i.Url, i.Order))
                        .FirstOrDefault() ?? new ImageDto(string.Empty, 0)
            )).ToList();
        }

        public async Task<SkuDto> GetSkuAsync(Guid productId, Guid skuId, CancellationToken ct = default)
        {
            // Проверяем что товар существует и виден
            var product = await productRepository.GetByIdAsync(productId, ct);
            if (product is null || !product.IsPubliclyVisible)
                throw new NotFoundException("Product", productId.ToString());

            var sku = await skuRepository.GetByIdAsync(productId, skuId, ct);
            if (sku is null)
                throw new NotFoundException("SKU", skuId.ToString());

            logger.LogDebug("Returning SKU {SkuId} for product {ProductId}", skuId, productId);

            return new SkuDto(
                sku.Id,
                sku.Name,
                sku.Price,
                sku.Quantity,
                sku.Characteristics.Select(c => new CharacteristicDto(c.Name, c.Value)).ToList(),
                sku.Images.OrderBy(i => i.Order).Select(i => new ImageDto(i.Url, i.Order)).ToList()
            );
        }
    }
}
