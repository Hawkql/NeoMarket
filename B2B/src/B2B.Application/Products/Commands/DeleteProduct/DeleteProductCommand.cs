using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2B.Application.Products.Commands.DeleteProduct
{
    public sealed record DeleteProductCommand(
    Guid ProductId,
    Guid SellerId
) : IRequest;
}
