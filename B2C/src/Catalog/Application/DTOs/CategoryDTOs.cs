using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    // ─── Category ──────────────────────────────────────────────────────────────────

    public record CategoryNodeDto(
        Guid Id,
        string Name,
        Guid? ParentId,
        IReadOnlyList<CategoryNodeDto> Children);

    public record CategoryTreeDto(IReadOnlyList<CategoryNodeDto> Items);

    public record CategoryParentDto(Guid Id, string Name, string Slug);

    public record CategorySeoDto(
        string Title,
        string Description,
        IReadOnlyList<string> Keywords);

    public record CategoryMetaDto(
        string? OgTitle,
        string? OgDescription,
        string? OgImage,
        string? TwitterCard);

    public record CategoryDetailDto(
        Guid Id,
        string Name,
        string Slug,
        string? Description,
        CategoryParentDto? Parent,
        int? ProductCount,
        CategorySeoDto Seo,
        CategoryMetaDto MetaTags,
        string? ImageUrl,
        bool IsActive,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    // ─── Filters ───────────────────────────────────────────────────────────────────

    public record FilterDto(
        string Slug,
        string Name,
        string Type,
        IReadOnlyList<object>? Value,
        decimal? Min,
        decimal? Max);

    public record FiltersListDto(IReadOnlyList<FilterDto> Items);

    // ─── Facets ────────────────────────────────────────────────────────────────────

    public record FacetValueDto(string Value, int Count);

    public record FacetDto(string Name, IReadOnlyList<FacetValueDto> Values);

    public record FacetsDto(Guid CategoryId, IReadOnlyList<FacetDto> Facets);

    // ─── Breadcrumbs ───────────────────────────────────────────────────────────────

    public record BreadcrumbItemDto(
        Guid Id,
        string Slug,
        string Name,
        string? Url,
        int Level,
        bool IsCurrent);

    public record BreadcrumbMetaDto(
        string ResolvedVia,
        Guid? CategoryId,
        Guid? ProductId);

    public record BreadcrumbDto(
        IReadOnlyList<BreadcrumbItemDto> Data,
        BreadcrumbMetaDto Meta);
}
