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
        public static CatalogProductCardDto ToCard(ProductSummary s) =>
             new(
                 Id: s.Id,
                 Name: s.Title,
                 Slug: null,                           // B2B пока не отдаёт; openapi optional
                 Category: null,                       // B2B summary не содержит category-ref; добавим когда B2B расширит
                 MinPrice: s.Price,
                 OldPrice: s.OldPrice,
                 HasStock: s.InStock,
                 Rating: s.Rating,
                 ReviewsCount: s.ReviewsCount ?? 0,
                 Images: s.ImageUrl is null
                        ? Array.Empty<ImageRefDto>()
                        : new[] { new ImageRefDto(
                            Id: GuidFromUrl(s.ImageUrl),
                            Url: s.ImageUrl,
                            Ordering: 0,
                            Alt: null) });

        public static CatalogProductDetailDto ToDetail(ProductDetail d) =>
             new(
                 // base card
                 Id: d.Id,
                 Name: d.Title,
                 Slug: null,
                 Category: new CategoryRefDto(
                        Id: d.CategoryId,
                        Name: string.Empty,       
                        ParentId: null,
                        Level: 0,
                        Path: Array.Empty<string>()),
                 MinPrice: d.Skus.Count > 0 ? d.Skus.Min(s => s.Price) : 0,
                 OldPrice: null,
                 HasStock: d.Skus.Any(s => s.InStock),
                 Rating: d.Rating,
                 ReviewsCount: d.ReviewsCount ?? 0,
                 Images: d.ImageUrls
                        .Select((u, i) => new ImageRefDto(
                            Id: GuidFromUrl(u),
                            Url: u,
                            Ordering: i,
                            Alt: null))
                        .ToList(),
                 // detail extensions
                 Description: d.Description,
                 Attributes: d.Characteristics.Count == 0
                     ? null
                     : d.Characteristics.ToDictionary(c => c.Name, c => c.Value),
                 Skus: d.Skus.Select(ToSkuDto).ToList());

        public static CatalogSkuDto ToSkuDto(SkuInfo sku) =>
             new(
                 Id: sku.Id,
                 Name: sku.Name,
                 SkuCode: null,                        // B2B пока не отдаёт SkuCode; openapi optional
                 Price: sku.Price,
                 OldPrice: null,                       // B2B вернёт когда добавит, сейчас null
                 AvailableQuantity: sku.AvailableQuantity,
                 Attributes: sku.Characteristics.Count == 0
                     ? null
                     : sku.Characteristics.ToDictionary(c => c.Name, c => c.Value),
                 Images: sku.ImageUrl is null
                        ? Array.Empty<ImageRefDto>()
                        : new[] { new ImageRefDto(
                            Id: GuidFromUrl(sku.ImageUrl),
                            Url: sku.ImageUrl,
                            Ordering: 0,
                            Alt: null) });

        public static CatalogCharacteristicDto ToCharacteristic(CharacteristicValue c) =>
            new(c.Name, c.Value);

        public static CategoryFiltersDto ToCategoryFilters(CategoryFilters f) =>
            new(f.Filters.Select(ToFilter).ToList(), f.PriceMin, f.PriceMax);

        public static CatalogFilterDto ToFilter(FilterDefinition f) =>
            new(f.Slug,
                f.Name,
                f.Values.Select(v => new CatalogFilterValueDto(v.Value, v.Count)).ToList());

        public static CategoryTreeNodeDto ToTreeNode(CategoryNode n) =>
            ToTreeNodeInternal(n, level: 0, parentPath: Array.Empty<string>());

        private static CategoryTreeNodeDto ToTreeNodeInternal(
            CategoryNode n, int level, IReadOnlyList<string> parentPath)
        {
            // path = путь предков + имя текущей ноды (по openapi — массив имён от корня).
            var path = parentPath.Append(n.Name).ToList();

            return new CategoryTreeNodeDto(
                Id: n.Id,
                Name: n.Name,
                ParentId: n.ParentId,
                Level: level,
                Path: path,
                Children: n.Children
                    .Select(c => ToTreeNodeInternal(c, level + 1, path))
                    .ToList());
        }

        public static BreadcrumbDto ToBreadcrumb(Breadcrumb b) =>
            new(b.CategoryId, b.Name, b.Slug);

        public static CatalogSort ToIntegrationSort(CatalogSortDto sort) => sort switch
        {
            CatalogSortDto.Popularity => CatalogSort.Popularity,
            CatalogSortDto.PriceAsc => CatalogSort.PriceAsc,
            CatalogSortDto.PriceDesc => CatalogSort.PriceDesc,
            CatalogSortDto.New => CatalogSort.New,
            _ => CatalogSort.Popularity,
        };
        public static IReadOnlyList<CategoryRefDto> ToFlatRefs(IReadOnlyList<CategoryNode> roots)
        {
            var result = new List<CategoryRefDto>();
            CollectFlat(roots, level: 0, parentPath: Array.Empty<string>(), acc: result);
            return result;
        }

        private static void CollectFlat(
            IReadOnlyList<CategoryNode> nodes,
            int level,
            IReadOnlyList<string> parentPath,
            List<CategoryRefDto> acc)
        {
            foreach (var n in nodes)
            {
                var path = parentPath.Append(n.Name).ToList();
                acc.Add(new CategoryRefDto(n.Id, n.Name, n.ParentId, level, path));

                if (n.Children is { Count: > 0 })
                    CollectFlat(n.Children, level + 1, path, acc);
            }
        }
        // Детерминированный uuid из строки URL (MD5 → Guid). Пока B2B не отдаёт
        // настоящий image_id — генерим стабильный из самой URL, чтобы рестарт сервиса
        // не менял id у того же изображения.
        private static Guid GuidFromUrl(string url)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(url));
            return new Guid(hash);
        }
    }
}
