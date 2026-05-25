using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Products.Dtos;
using MediatR;

namespace B2B.Application.Products.Commands.UpdateProduct
{
    public sealed record UpdateProductCommand(
        Guid ProductId,
        Guid SellerId,
        string? Title,
        string? Description,
        Guid? CategoryId,
        IReadOnlyList<CharacteristicInputDto>? Characteristics
    ) : IRequest<ProductDto>;
}
