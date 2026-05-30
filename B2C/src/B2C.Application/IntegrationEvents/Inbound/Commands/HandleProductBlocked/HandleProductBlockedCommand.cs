using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2C.Application.IntegrationEvents.Inbound.Commands.HandleProductBlocked
{
    /// <summary>
    /// Команда на обработку входящего события ProductBlocked от B2B.
    /// 
    /// Поле IdempotencyKey пробрасывается из исходного события — по нему делается дедупликация
    /// (Inbox Pattern): второй вызов с тем же ключом отрабатывает no-op.
    /// </summary>
    public sealed record HandleProductBlockedCommand(
        Guid IdempotencyKey,
        Guid ProductId,
        Guid[] SkuIds) : IRequest;
}
