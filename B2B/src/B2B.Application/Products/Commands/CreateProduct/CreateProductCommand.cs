using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Products.Dtos;
using MediatR;

namespace B2B.Application.Products.Commands.CreateProduct
{
    public sealed record CreateProductCommand(
    Guid SellerId,
    Guid CategoryId,
    string Title,
    string Description,
    IReadOnlyList<ImageInputDto> Images,
    IReadOnlyList<CharacteristicInputDto> Characteristics
) : IRequest<ProductDto>;
}
