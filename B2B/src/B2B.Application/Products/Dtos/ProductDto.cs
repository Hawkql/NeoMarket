using System;
using System.Collections.Generic;
using B2B.Application.Skus.Dtos;
using B2B.Domain.Products;

namespace B2B.Application.Products.Dtos
{
    /// <summary>
    /// Seller-view detail карточки. Соответствует ProductDetailResponse в openapi.yaml.
    /// blocking_reason — вложенный объект {id, title, comment} согласно схеме BlockingReason.
    /// </summary>
    public sealed record ProductDto(
       Guid Id,
       Guid SellerId,
       Guid CategoryId,
       string Title,
       string Slug,
       string Description,
       ProductStatus Status,
       bool Deleted,
       IReadOnlyList<ImageDto> Images,
       IReadOnlyList<CharacteristicDto> Characteristics,
       IReadOnlyList<SkuResponseDto> Skus,
       DateTime CreatedAt,
       DateTime UpdatedAt,
       bool Blocked,
       BlockingReasonDto? BlockingReason,
       IReadOnlyList<FieldReportDto> FieldReports);

    public sealed record ImageDto(Guid Id, string Url, int Ordering);
    public sealed record CharacteristicDto(Guid Id, string Name, string Value);
    public sealed record BlockingReasonDto(Guid Id, string Title, string Comment);
    public sealed record FieldReportDto(string FieldName, Guid? SkuId, string Comment);
}