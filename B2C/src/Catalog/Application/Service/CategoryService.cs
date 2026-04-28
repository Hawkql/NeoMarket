using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Repository;
using Microsoft.Extensions.Logging;

namespace Application.Service
{
    public sealed class CategoryService(
    ICategoryRepository categoryRepository,
    ILogger<CategoryService> logger) : ICategoryService
    {
        public async Task<CategoryTreeDto> GetTreeAsync(CancellationToken ct = default)
        {
            var flat = await categoryRepository.GetAllFlatAsync(ct);

            logger.LogDebug("Building category tree from {Count} nodes", flat.Count);

            return BuildTree(flat);
        }

        public async Task<CategoryDetailDto> GetDetailAsync(
            Guid id,
            bool includeProductCount,
            string lang,
            CancellationToken ct = default)
        {
            var category = await categoryRepository.GetByIdAsync(id, ct);
            if (category is null)
                throw new NotFoundException("Category", id.ToString());

            int? productCount = null;
            if (includeProductCount)
                productCount = await categoryRepository.CountProductsAsync(id, ct);

            logger.LogDebug("Category {CategoryId} detail loaded, productCount={Count}", id, productCount);

            return MapToDetailDto(category, productCount);
        }

        // ─── Tree builder — O(n) ────────────────────────────────────────────────────

        /// <summary>
        /// Строим дерево из плоского списка за O(n).
        /// Ключевой паттерн: Dictionary lookup вместо вложенных циклов.
        /// </summary>
        private static CategoryTreeDto BuildTree(List<Category> flat)
        {
            if (flat.Count == 0)
                return new CategoryTreeDto([]);

            // Строим DTO-словарь с изменяемыми коллекциями Children
            var dtoMap = flat.ToDictionary(
                c => c.Id,
                c => new CategoryNodeDto(c.Id, c.Name, c.ParentId, new List<CategoryNodeDto>()));

            var roots = new List<CategoryNodeDto>();

            foreach (var cat in flat)
            {
                var dto = dtoMap[cat.Id];

                if (cat.ParentId.HasValue && dtoMap.TryGetValue(cat.ParentId.Value, out var parentDto))
                    ((List<CategoryNodeDto>)parentDto.Children).Add(dto);
                else
                    roots.Add(dto);
            }

            return new CategoryTreeDto(roots);
        }

        // ─── Mapping ────────────────────────────────────────────────────────────────

        private static CategoryDetailDto MapToDetailDto(Category c, int? productCount)
        {
            string[] keywords;
            try
            {
                keywords = JsonSerializer.Deserialize<string[]>(c.SeoKeywordsJson) ?? [];
            }
            catch
            {
                keywords = [];
            }

            CategoryParentDto? parentDto = null;
            if (c.Parent is not null)
                parentDto = new CategoryParentDto(c.Parent.Id, c.Parent.Name, c.Parent.Slug);

            return new CategoryDetailDto(
                c.Id,
                c.Name,
                c.Slug,
                c.Description,
                parentDto,
                productCount,
                new CategorySeoDto(c.SeoTitle, c.SeoDescription, keywords),
                new CategoryMetaDto(c.OgTitle, c.OgDescription, c.OgImage, c.TwitterCard),
                c.ImageUrl,
                c.IsActive,
                c.CreatedAt,
                c.UpdatedAt);
        }
    }
}
