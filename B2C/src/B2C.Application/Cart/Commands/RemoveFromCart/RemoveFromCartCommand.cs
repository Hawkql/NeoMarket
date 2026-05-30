using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2C.Application.Cart.Commands.RemoveFromCart
{
    public sealed record RemoveFromCartCommand(Guid SkuId) : IRequest;
}
