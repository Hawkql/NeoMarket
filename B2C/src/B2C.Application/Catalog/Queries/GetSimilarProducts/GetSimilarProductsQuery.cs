using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Catalog.Dtos;
using MediatR;

namespace B2C.Application.Catalog.Queries.GetSimilarProducts
{
    public sealed record GetSimilarProductsQuery(
       Guid ProductId,
       int Limit) : IRequest<IReadOnlyList<CatalogProductCardDto>>;
}
