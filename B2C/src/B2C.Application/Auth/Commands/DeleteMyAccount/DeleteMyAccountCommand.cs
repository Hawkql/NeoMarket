using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2C.Application.Auth.Commands.DeleteMyAccount
{
    /// <summary>
    /// Удаление собственного аккаунта (soft delete).
    /// BuyerId берётся из ICurrentUserService — параметров нет.
    /// </summary>
    public sealed record DeleteMyAccountCommand : IRequest;
}
