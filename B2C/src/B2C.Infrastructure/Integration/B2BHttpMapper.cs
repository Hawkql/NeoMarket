using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Integration.Dtos;

using B2C.Infrastructure.Integration.Contracts;

namespace B2C.Infrastructure.Integration
{
    internal static class B2BHttpMapper
    {
        public static ProductSummary ToProductSummary(B2bProductSummaryResponse r) =>
            new(r.Id, r.Title, r.ImageUrl, r.Price, r.OldPrice, r.Discount,
                r.InStock, r.Rating, r.ReviewsCount);

        public static SkuInfo ToSkuInfo(B2bSkuResponse r) =>
            new(r.Id,
                r.ProductId,
                r.Name,
                r.Price,
                r.Discount,
                r.ImageUrl,
                InStock: r.ActiveQuantity > 0,   // ← ACL: количество → bool
                r.Characteristics.Select(ToCharacteristic).ToList());

        public static CharacteristicValue ToCharacteristic(B2bCharacteristicResponse r) =>
            new(r.Name, r.Value);

        public static ProductDetail ToProductDetail(B2bProductDetailResponse r) =>
            new(r.Id,
                r.Title,
                r.Description,
                r.CategoryId,
                r.ImageUrls,
                r.Skus.Select(ToSkuInfo).ToList(),
                r.Characteristics.Select(ToCharacteristic).ToList(),
                r.Rating,
                r.ReviewsCount);

        public static CatalogPage ToCatalogPage(B2bCatalogPageResponse r) =>
            new(r.Items.Select(ToProductSummary).ToList(), r.TotalCount, r.Limit, r.Offset);

        public static CategoryNode ToCategoryNode(B2bCategoryNodeResponse r) =>
            new(r.Id, r.ParentId, r.Name, r.Slug,
                r.Children.Select(ToCategoryNode).ToList());

        public static Breadcrumb ToBreadcrumb(B2bBreadcrumbResponse r) =>
            new(r.CategoryId, r.Name, r.Slug);

        public static CategoryFilters ToCategoryFilters(B2bCategoryFiltersResponse r) =>
            new(r.Filters.Select(ToFilterDefinition).ToList(), r.PriceMin, r.PriceMax);

        public static FilterDefinition ToFilterDefinition(B2bFilterDefinitionResponse r) =>
            new(r.Slug, r.Name,
                r.Values.Select(v => new FilterValue(v.Value, v.Count)).ToList());

        public static ReserveResult ToReserveResult(B2bReserveResponse r) =>
            new(r.Success,
                r.FailedItems.Select(ToReserveFailedItem).ToList());

        public static ReserveFailedItem ToReserveFailedItem(B2bReserveFailedItem r) =>
            new(r.SkuId, r.Requested, r.Available, ParseReason(r.Reason));

        private static ReserveFailReason ParseReason(string reason) => reason switch
        {
            "out_of_stock" => ReserveFailReason.OutOfStock,
            "insufficient_stock" => ReserveFailReason.InsufficientStock,
            "product_blocked" => ReserveFailReason.ProductBlocked,
            "product_deleted" => ReserveFailReason.ProductDeleted,
            "sku_not_found" => ReserveFailReason.SkuNotFound,
            _ => ReserveFailReason.OutOfStock,  // безопасный default
        };
    }
}
