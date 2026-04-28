using System.Text.Json.Serialization;

namespace Api.DTOs
{
    // ─── Shared ────────────────────────────────────────────────────────────────────

    public record ImageResponse(string Url, int Order);
    public record CharacteristicResponse(string Name, string Value);
    public record ErrorResponse(string Message);

    // ─── Product ───────────────────────────────────────────────────────────────────

    public record SkuShortResponse(
        string Name,
        decimal Price,
        ImageResponse Image);

    public record SkuResponse(
        Guid Id,
        string Name,
        decimal Price,
        int Quantity,
        IReadOnlyList<CharacteristicResponse> Characteristics,
        IReadOnlyList<ImageResponse>? Images);

    public record ProductResponse(
        Guid Id,
        string Slug,
        string Title,
        string Description,
        IReadOnlyList<ImageResponse> Images,
        string Status,
        IReadOnlyList<CharacteristicResponse> Characteristics,
        IReadOnlyList<SkuResponse> Skus);

    public record ProductShortResponse(
        Guid Id,
        string Title,
        string Image,
        decimal Price,
        [property: JsonPropertyName("in_stock")] bool InStock,
        [property: JsonPropertyName("is_in_cart")] bool IsInCart);

    public record ProductListResponse(
        [property: JsonPropertyName("total_count")] int TotalCount,
        int Limit,
        int Offset,
        IReadOnlyList<ProductShortResponse> Items);

    // ─── Category ──────────────────────────────────────────────────────────────────

    public record CategoryNodeResponse(
        Guid Id,
        string Name,
        [property: JsonPropertyName("parent_id")] Guid? ParentId,
        IReadOnlyList<CategoryNodeResponse> Children);

    public record CategoryTreeResponse(IReadOnlyList<CategoryNodeResponse> Items);

    public record CategoryParentResponse(Guid Id, string Name, string Slug);

    public record CategorySeoResponse(
        string Title,
        string Description,
        IReadOnlyList<string> Keywords);

    public record CategoryMetaResponse(
        [property: JsonPropertyName("og_title")] string? OgTitle,
        [property: JsonPropertyName("og_description")] string? OgDescription,
        [property: JsonPropertyName("og_image")] string? OgImage,
        [property: JsonPropertyName("twitter_card")] string? TwitterCard);

    public record CategoryDetailResponse(
        Guid Id,
        string Name,
        string Slug,
        string? Description,
        CategoryParentResponse? Parent,
        [property: JsonPropertyName("product_count")] int? ProductCount,
        CategorySeoResponse Seo,
        [property: JsonPropertyName("meta_tags")] CategoryMetaResponse MetaTags,
        [property: JsonPropertyName("image_url")] string? ImageUrl,
        [property: JsonPropertyName("is_active")] bool IsActive,
        [property: JsonPropertyName("created_at")] DateTime CreatedAt,
        [property: JsonPropertyName("updated_at")] DateTime UpdatedAt);

    // ─── Filters / Facets ──────────────────────────────────────────────────────────

    public record FilterResponse(
        string Slug,
        string Name,
        string Type,
        IReadOnlyList<object>? Value,
        decimal? Min,
        decimal? Max);

    public record FiltersResponse(IReadOnlyList<FilterResponse> Items);

    public record FacetValueResponse(string Value, int Count);
    public record FacetResponse(string Name, IReadOnlyList<FacetValueResponse> Values);

    public record FacetsResponse(
        [property: JsonPropertyName("category_id")] Guid CategoryId,
        IReadOnlyList<FacetResponse> Facets);

    // ─── Breadcrumbs ───────────────────────────────────────────────────────────────

    public record BreadcrumbItemResponse(
        Guid Id,
        string Slug,
        string Name,
        string? Url,
        int Level,
        [property: JsonPropertyName("is_current")] bool IsCurrent);

    public record BreadcrumbMetaResponse(
        [property: JsonPropertyName("resolved_via")] string ResolvedVia,
        [property: JsonPropertyName("category_id")] Guid? CategoryId,
        [property: JsonPropertyName("product_id")] Guid? ProductId);

    public record BreadcrumbResponse(
        IReadOnlyList<BreadcrumbItemResponse> Data,
        BreadcrumbMetaResponse Meta);
}
