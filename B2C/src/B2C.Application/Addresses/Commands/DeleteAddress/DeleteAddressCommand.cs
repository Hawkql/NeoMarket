using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2C.Application.Addresses.Commands.DeleteAddress
{
    public sealed record DeleteAddressCommand(Guid AddressId) : IRequest;
}
