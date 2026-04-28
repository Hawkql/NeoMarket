using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;
namespace Application.DTOs
{
    
 
    public record ImageDto(string Url, int Order);

        public record CharacteristicDto(string Name, string Value);

        // ─── SKU ──────────────────────────────────────────────────────────────────────

        public record SkuDto(
            Guid Id,
            string Name,
            decimal Price,
            int Quantity,
            IReadOnlyList<CharacteristicDto> Characteristics,
            IReadOnlyList<ImageDto>? Images
        );

        public record SkuShortDto(
            string Name,
            decimal Price,
            ImageDto Image
        );

        // ─── Полный товар ─────────────────────────────────────────────────────────────

        public record ProductDto(
            Guid Id,
            string Slug,
            string Title,
            string Description,
            IReadOnlyList<ImageDto> Images,
            string Status,
            IReadOnlyList<CharacteristicDto> Characteristics,
            IReadOnlyList<SkuDto> Skus
        );

        // ─── Краткий товар (листинг) ──────────────────────────────────────────────────

        public record ProductShortDto(
            Guid Id,
            string Title,
            string Image,
            decimal Price,
            bool InStock,
            bool IsInCart
        );

        // ─── Листинг товаров ─────────────────────────────────────────────────────────

        public record ProductListDto(
            int TotalCount,
            int Limit,
            int Offset,
            IReadOnlyList<ProductShortDto> Items
        );

        // ─── Фильтр листинга ─────────────────────────────────────────────────────────

        public record ProductListFilterDto(
            Guid? CategoryId,
            string? Search,
            Dictionary<string, object?>? Filters,
            SortOption? Sort,
            int Limit = 20,
            int Offset = 0
        );

        // ─── Похожие товары ───────────────────────────────────────────────────────────

        public record SimilarProductsFilterDto(
            Guid ProductId,
            Guid CategoryId,
            int Limit = 8,
            int Offset = 0
        );
}
