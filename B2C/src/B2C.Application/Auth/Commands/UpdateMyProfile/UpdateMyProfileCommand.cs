using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2C.Application.Auth.Commands.UpdateMyProfile
{
    /// <summary>
    /// PATCH профиля. Все поля опциональны — null означает "не менять"
    /// (соответствует Buyer.UpdateProfile semantics).
    /// 
    /// BuyerId берётся из ICurrentUserService в handler'е, НЕ из request body
    /// (правило IDOR — никогда не доверяем пользователю в указании "кого" обновлять).
    /// </summary>
    public sealed record UpdateMyProfileCommand(
        string? FirstName,
        string? LastName,
        string? Phone) : IRequest;
}
