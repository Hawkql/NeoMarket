using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Products.Dtos;
using MediatR;

namespace B2B.Application.Products.Queries.GetProductById
{
    public sealed record GetProductByIdQuery(
        Guid ProductId,
        Guid SellerId
    ) : IRequest<ProductDto>;
}
