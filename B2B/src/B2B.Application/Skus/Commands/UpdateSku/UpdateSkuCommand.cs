using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Skus.Dtos;
using MediatR;

namespace B2B.Application.Skus.Commands.UpdateSku
{
    public sealed record UpdateSkuCommand(
        Guid SkuId,
        Guid SellerId,
        string? Name,
        int? Price,
        int? Discount,
        int? CostPrice,
        string? Article,
        IReadOnlyList<SkuCharacteristicInputDto>? Characteristics
    ) : IRequest<SkuResponseDto>;
}
