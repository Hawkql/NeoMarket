using B2B.Application.Products.Dtos;

namespace B2B.Api.Contracts
{
    public sealed record CreateProductRequest(
    Guid CategoryId,
    string Title,
    string Description,
    IReadOnlyList<CharacteristicInputDto>? Characteristics,
    IReadOnlyList<ImageInputDto>? Images);

    public sealed record UpdateProductRequest(
        string? Title,
        string? Description,
        Guid? CategoryId,
        IReadOnlyList<CharacteristicInputDto>? Characteristics);
}
