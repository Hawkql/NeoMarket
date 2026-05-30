using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Auth.Dtos;
using MediatR;

namespace B2C.Application.Auth.Commands.Register
{
    /// <summary>
    /// Регистрация нового покупателя. По OpenAPI возвращает сразу токены
    /// (автологин после регистрации — UX-конвенция B2C-площадок).
    /// </summary>
    public sealed record BuyerRegisterCommand(
        string Email,
        string Password,
        string? FirstName,
        string? LastName,
        string? Phone) : IRequest<TokenResponseDto>;
}
