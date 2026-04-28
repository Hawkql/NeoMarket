using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repository;
using Microsoft.Extensions.Logging;

namespace Application.Service
{
    public sealed class ProductService(
    IProductRepository productRepository,
    ILogger<ProductService> logger) : IProductService
    {
        public async Task<ProductDto> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var product = await productRepository.GetByIdAsync(id, ct);

            if (product is null)
                throw new NotFoundException("Product", id.ToString());

            // Бизнес-правило: публичная витрина видит только прошедшие модерацию
            if (!product.IsPubliclyVisible)
                throw new NotFoundException("Product", id.ToString());

            logger.LogDebug("Product {ProductId} found and visible", id);
            return MapToProductDto(product);
        }

        public async Task<ProductListDto> ListAsync(
            Guid? categoryId,
            string? search,
            Dictionary<string, string>? filters,
            string? sort,
            int limit,
            int offset,
            CancellationToken ct = default)
        {
            // Нормализуем пагинацию
            limit = Math.Clamp(limit, 1, 100);
            offset = Math.Max(offset, 0);

            var (items, total) = await productRepository.ListAsync(
                categoryId, search, filters, sort, limit, offset, ct);

            logger.LogDebug("Product list: total={Total}, returned={Count}", total, items.Count);

            return new ProductListDto(
                total,
                limit,
                offset,
                items.Select(MapToProductShortDto).ToList());
        }

        public async Task<ProductListDto> GetSimilarAsync(
            Guid productId,
            Guid categoryId,
            int limit,
            int offset,
            CancellationToken ct = default)
        {
            // Проверяем существование исходного товара
            var source = await productRepository.GetByIdAsync(productId, ct);
            if (source is null || !source.IsPubliclyVisible)
                throw new NotFoundException("Product", productId.ToString());

            limit = Math.Clamp(limit, 1, 50);
            offset = Math.Max(offset, 0);

            var (items, total) = await productRepository.GetSimilarAsync(
                productId, categoryId, limit, offset, ct);

            return new ProductListDto(total, limit, offset, items.Select(MapToProductShortDto).ToList());
        }

        // ─── Mapping ──────────────────────────────────────────────────────────────

        private static ProductDto MapToProductDto(Product p) => new(
            p.Id,
            p.Slug,
            p.Title,
            p.Description,
            p.Images.OrderBy(i => i.Order)
                    .Select(i => new ImageDto(i.Url, i.Order)).ToList(),
            p.Status.ToString().ToUpperInvariant(),
            p.Characteristics.Select(c => new CharacteristicDto(c.Name, c.Value)).ToList(),
            p.Skus.Select(s => new SkuDto(
                s.Id,
                s.Name,
                s.Price,
                s.Quantity,
                s.Characteristics.Select(c => new CharacteristicDto(c.Name, c.Value)).ToList(),
                s.Images.OrderBy(i => i.Order).Select(i => new ImageDto(i.Url, i.Order)).ToList()
            )).ToList()
        );

        private static ProductShortDto MapToProductShortDto(Product p) => new(
            p.Id,
            p.Title,
            p.Images.OrderBy(i => i.Order).FirstOrDefault()?.Url ?? string.Empty,
            p.MinPrice,
            p.HasStock,
            false // IsInCart обогащается Cart Service, здесь всегда false
        );
    }
}
