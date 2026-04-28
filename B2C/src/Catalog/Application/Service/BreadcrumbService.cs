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
    public sealed class BreadcrumbService(
    ICategoryRepository categoryRepository,
    ILogger<BreadcrumbService> logger) : IBreadcrumbService
    {
        public async Task<BreadcrumbDto> BuildAsync(
            Guid? categoryId,
            Guid? productId,
            string lang,
            CancellationToken ct = default)
        {
            // Валидация: ровно один из параметров
            if (categoryId is null && productId is null)
                throw new MissingParameterException("category_id or product_id");

            if (categoryId is not null && productId is not null)
                throw new AmbiguousParameterException("category_id", "product_id");

            Guid resolvedCategoryId;
            string resolvedVia;

            if (productId.HasValue)
            {
                var catId = await categoryRepository.GetCategoryIdByProductAsync(productId.Value, ct);
                if (catId is null)
                    throw new NotFoundException("Product", productId.Value.ToString());

                resolvedCategoryId = catId.Value;
                resolvedVia = "product_id";
            }
            else
            {
                resolvedCategoryId = categoryId!.Value;
                resolvedVia = "category_id";
            }

            // Получаем цепочку предков от корня до текущего узла
            var ancestors = await categoryRepository.GetAncestorsAsync(resolvedCategoryId, ct);

            if (ancestors.Count == 0)
                throw new NotFoundException("Category", resolvedCategoryId.ToString());

            // Проверяем целостность иерархии
            ValidateChain(ancestors);

            var items = BuildItems(ancestors, resolvedCategoryId, resolvedVia);

            logger.LogDebug("Breadcrumbs built: {Count} items via {Via}", items.Count, resolvedVia);

            return new BreadcrumbDto(
                items,
                new BreadcrumbMetaDto(resolvedVia, categoryId, productId));
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private static List<BreadcrumbItemDto> BuildItems(
            List<Category> ancestors, Guid leafCategoryId, string resolvedVia)
        {
            return ancestors.Select((cat, idx) => new BreadcrumbItemDto(
                cat.Id,
                cat.Slug,
                cat.Name,
                BuildUrl(ancestors, idx),
                idx,
                // Текущим является последний элемент только при resolvedVia = category_id
                resolvedVia == "category_id" && cat.Id == leafCategoryId
            )).ToList();
        }

        private static string BuildUrl(List<Category> ancestors, int upToIndex)
        {
            var slugs = ancestors.Take(upToIndex + 1).Select(c => c.Slug);
            return "/catalog/" + string.Join("/", slugs);
        }

        /// <summary>Проверяем что цепочка непрерывна — нет «осиротевших» узлов.</summary>
        private static void ValidateChain(List<Category> ancestors)
        {
            if (ancestors[0].ParentId is not null)
                throw new OrphanNodeException(ancestors[0].Id.ToString());

            for (int i = 1; i < ancestors.Count; i++)
            {
                if (ancestors[i].ParentId != ancestors[i - 1].Id)
                    throw new OrphanNodeException(ancestors[i].Id.ToString());
            }
        }
    }
}
