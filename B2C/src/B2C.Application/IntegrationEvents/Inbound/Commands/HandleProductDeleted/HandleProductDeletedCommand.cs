using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2C.Application.IntegrationEvents.Inbound.Commands.HandleProductDeleted
{
    public sealed record HandleProductDeletedCommand(
        Guid IdempotencyKey,
        Guid ProductId,
        Guid[] SkuIds) : IRequest;
}
