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
    public sealed class CategoryFilterService(
    ICategoryFilterRepository filterRepository,
    ICategoryRepository categoryRepository,
    ILogger<CategoryFilterService> logger) : ICategoryFilterService
    {
        public async Task<FiltersListDto> GetFiltersAsync(Guid categoryId, CancellationToken ct = default)
        {
            // Проверяем существование категории
            var category = await categoryRepository.GetByIdAsync(categoryId, ct);
            if (category is null)
                throw new NotFoundException("Category", categoryId.ToString());

            var filters = await filterRepository.GetByCategoryIdAsync(categoryId, ct);

            logger.LogDebug("Returning {Count} filters for category {CategoryId}", filters.Count, categoryId);

            return new FiltersListDto(filters.Select(MapToFilterDto).ToList());
        }

        public async Task<FacetsDto> GetFacetsAsync(
            Guid categoryId,
            Dictionary<string, string>? activeFilters,
            CancellationToken ct = default)
        {
            if (categoryId == Guid.Empty)
                throw new ValidationException("categoryId", "must not be empty");

            var category = await categoryRepository.GetByIdAsync(categoryId, ct);
            if (category is null)
                throw new NotFoundException("Category", categoryId.ToString());

            var facets = await filterRepository.GetFacetsAsync(categoryId, activeFilters, ct);

            // Группируем кортежи по имени фильтра
            var grouped = facets
                .GroupBy(f => f.FilterName)
                .Select(g => new FacetDto(
                    g.Key,
                    g.Select(v => new FacetValueDto(v.Value, v.Count)).ToList()))
                .ToList();

            logger.LogDebug("Returning {Count} facets for category {CategoryId}", grouped.Count, categoryId);

            return new FacetsDto(categoryId, grouped);
        }

        // ─── Mapping ────────────────────────────────────────────────────────────────

        private static FilterDto MapToFilterDto(CategoryFilter f)
        {
            List<object>? values = null;

            if (!string.IsNullOrWhiteSpace(f.ValuesJson))
            {
                try
                {
                    values = JsonSerializer.Deserialize<List<object>>(f.ValuesJson);
                }
                catch
                {
                    // malformed JSON — возвращаем null, не падаем
                }
            }

            return new FilterDto(f.Slug, f.Name, f.FilterType, values, f.MinValue, f.MaxValue);
        }
    }
}
