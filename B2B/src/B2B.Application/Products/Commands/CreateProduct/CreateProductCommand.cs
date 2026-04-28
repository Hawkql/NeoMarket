using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2B.Application.Products.Commands.CreateProduct
{
    public record CreateProductCommand(
        string Title,
        string Description,
        Guid CategoryId,
        Guid SelleryId,
        List<CharacteristicDto> Characteristics) : IRequest<Guid> { }
    public record CharacteristicDto (string Name,string Value) { }
}
