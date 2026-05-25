using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.PublicCatalog.Dtos;
using MediatR;

namespace B2B.Application.PublicCatalog.Queries.BatchPublicProducts
{
    public sealed record BatchPublicProductsQuery(
        IReadOnlyList<Guid> ProductIds
    ) : IRequest<IReadOnlyList<ProductPublicDto>>;
}
