using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Integration.Dtos;


namespace B2C.Application.Catalog.Dtos
{
    internal static class CatalogMapper
    {
        public static CatalogProductCardDto ToCard(ProductSummary p) =>
            new(p.Id, p.Title, p.ImageUrl, p.Price, p.OldPrice, p.Discount,
                p.InStock, p.Rating, p.ReviewsCount);

        public static CatalogProductDetailDto ToDetail(ProductDetail p) =>
            new(p.Id,
                p.Title,
                p.Description,
                p.CategoryId,
                p.ImageUrls,
                p.Skus.Select(ToSku).ToList(),
                p.Characteristics.Select(ToCharacteristic).ToList(),
                p.Rating,
                p.ReviewsCount);

        public static CatalogSkuDto ToSku(SkuInfo s) =>
            new(s.Id,
                s.Name,
                s.Price,
                s.Discount,
                s.ImageUrl,
                s.InStock,
                s.Characteristics.Select(ToCharacteristic).ToList());

        public static CatalogCharacteristicDto ToCharacteristic(CharacteristicValue c) =>
            new(c.Name, c.Value);

        public static CategoryFiltersDto ToCategoryFilters(CategoryFilters f) =>
            new(f.Filters.Select(ToFilter).ToList(), f.PriceMin, f.PriceMax);

        public static CatalogFilterDto ToFilter(FilterDefinition f) =>
            new(f.Slug,
                f.Name,
                f.Values.Select(v => new CatalogFilterValueDto(v.Value, v.Count)).ToList());

        public static CategoryTreeNodeDto ToTreeNode(CategoryNode n) =>
            new(n.Id,
                n.ParentId,
                n.Name,
                n.Slug,
                n.Children.Select(ToTreeNode).ToList());

        public static BreadcrumbDto ToBreadcrumb(Breadcrumb b) =>
            new(b.CategoryId, b.Name, b.Slug);

        public static CatalogSort ToIntegrationSort(CatalogSortDto dto) => dto switch
        {
            CatalogSortDto.Rating => CatalogSort.Rating,
            CatalogSortDto.Popularity => CatalogSort.Popularity,
            CatalogSortDto.PriceAsc => CatalogSort.PriceAsc,
            CatalogSortDto.PriceDesc => CatalogSort.PriceDesc,
            CatalogSortDto.DateDesc => CatalogSort.DateDesc,
            CatalogSortDto.DiscountDesc => CatalogSort.DiscountDesc,
            _ => CatalogSort.Rating,
        };
    }
}
