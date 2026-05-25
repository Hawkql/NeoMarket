using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2B.Application.Skus.Commands.DeleteSku
{
    public sealed record DeleteSkuCommand(
    Guid SkuId,
    Guid SellerId
) : IRequest;
}
