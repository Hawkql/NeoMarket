using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Application.Skus.Dtos;
using MediatR;

namespace B2B.Application.Skus.Commands.СreateSku
{
    public sealed record CreateSkuCommand(
        Guid SellerId,
        Guid ProductId,
        string Name,
        int Price,
        int Discount,
        int? CostPrice,
        string? Article,
        IReadOnlyList<SkuImageInputDto> Images,
        IReadOnlyList<SkuCharacteristicInputDto> Characteristics
    ) : IRequest<SkuResponseDto>;
}
