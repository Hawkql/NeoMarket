using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Addresses.Dtos;
using MediatR;

namespace B2C.Application.Addresses.Queries.GetMyAddress
{
    public sealed record GetMyAddressQuery(Guid AddressId) : IRequest<AddressDto>;
}
