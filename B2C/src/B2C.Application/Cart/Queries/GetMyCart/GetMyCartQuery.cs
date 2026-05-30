using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Cart.Dtos;
using MediatR;

namespace B2C.Application.Cart.Queries.GetMyCart
{
    public sealed record GetMyCartQuery : IRequest<CartDto>;
}
