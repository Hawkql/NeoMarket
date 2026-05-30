using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Auth.Dtos;
using MediatR;

namespace B2C.Application.Auth.Queries.GetMyProfile
{
    public sealed record GetMyProfileQuery : IRequest<BuyerProfileDto>;
}
