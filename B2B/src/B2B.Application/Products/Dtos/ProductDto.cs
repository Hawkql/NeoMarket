using System;
using System.Collections.Generic;
using B2B.Application.Skus.Dtos;
using B2B.Domain.Products;

namespace B2B.Application.Products.Dtos
{
    public sealed record ProductDto(
       Guid Id,
       Guid SellerId,
       Guid CategoryId,
       string Title,
       string Slug,
       string Description,
       ProductStatus Status,
       bool Deleted,
       Guid? BlockingReasonId,            // ← по OpenAPI ProductResponse
       string? ModeratorComment,          // ← по OpenAPI ProductResponse
       IReadOnlyList<ImageDto> Images,
       IReadOnlyList<CharacteristicDto> Characteristics,
       IReadOnlyList<SkuResponseDto> Skus,
       DateTime CreatedAt,
       DateTime UpdatedAt,
       bool Blocked,
       IReadOnlyList<FieldReportDto> FieldReports);

    public sealed record ImageDto(Guid Id, string Url, int Ordering);
    public sealed record CharacteristicDto(Guid Id, string Name, string Value);
    public sealed record FieldReportDto(string FieldName, Guid? SkuId, string Comment);
}