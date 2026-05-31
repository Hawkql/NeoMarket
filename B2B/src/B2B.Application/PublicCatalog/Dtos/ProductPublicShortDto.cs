using B2B.Domain.Products;

namespace B2B.Application.PublicCatalog.Dtos
{
    public sealed record ProductPublicShortDto(
    Guid Id,
    string Title,
    string Slug,
    ProductStatus Status,
    Guid CategoryId,
    int MinPrice,
    string? CoverImage,
    DateTime CreatedAt);
}
