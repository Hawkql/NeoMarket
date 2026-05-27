using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
       Guid? BlockingReasonId,
       string? ModeratorComment,
       IReadOnlyList<ImageDto> Images,
       IReadOnlyList<CharacteristicDto> Characteristics,
       IReadOnlyList<SkuDto> Skus,
       DateTime CreatedAt,
       DateTime UpdatedAt,
       bool Blocked,
       BlockingReasonDto? BlockingReason,
       IReadOnlyList<FieldReportDto> FieldReports);

    public sealed record ImageDto(Guid Id, string Url, int Ordering);
    public sealed record CharacteristicDto(Guid Id, string Name, string Value);
    public sealed record BlockingReasonDto(Guid Id, string Title, string? Comment);
    public sealed record FieldReportDto(string FieldName, Guid? SkuId, string Comment);
}
   