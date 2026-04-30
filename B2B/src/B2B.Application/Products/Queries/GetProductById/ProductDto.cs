using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace B2B.Application.Products.Queries.GetProductById
{

    public record ProductDto(
        Guid Id,
        string Title,
        string Description,
        string Slug,
        Guid CategoryId,
        int Status,
        List<SkuDto> Skus,
        List<CharacteristicDto> Characteristics,
        List<ImageDto> Images);

    public record SkuDto(
        Guid Id,
        string Name,
        decimal Price,
        int Quantity,
        List<CharacteristicDto> Characteristics);

    public record CharacteristicDto(string Name, string Value);

    public record ImageDto(string Url, int Order);
}
