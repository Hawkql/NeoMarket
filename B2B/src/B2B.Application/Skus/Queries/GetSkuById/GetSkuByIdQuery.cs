using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Skus.Dtos;
using MediatR;

namespace B2B.Application.Skus.Queries.GetSkuById
{
    public sealed record GetSkuByIdQuery(
    Guid SkuId,
    Guid SellerId
) : IRequest<SkuResponseDto>;
}
