using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.PublicCatalog.Dtos;
using MediatR;

namespace B2B.Application.PublicCatalog.Queries.GetPublicProduct
{
    public sealed record GetPublicProductQuery(Guid ProductId)
     : IRequest<ProductPublicDto>;
}
