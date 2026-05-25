using B2B.Application.Skus.Dtos;

namespace B2B.Api.Contracts
{
    public sealed record CreateSkuRequest(
        Guid ProductId,
        string Name,
        int Price,
        int Discount,
        int? CostPrice,
        string? Article,
        IReadOnlyList<SkuImageInputDto>? Images,
        IReadOnlyList<SkuCharacteristicInputDto>? Characteristics);

    public sealed record UpdateSkuRequest(
        string? Name,
        int? Price,
        int? Discount,
        int? CostPrice,
        string? Article,
        IReadOnlyList<SkuCharacteristicInputDto>? Characteristics);
}
