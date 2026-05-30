using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2C.Application.Auth.Dtos;
using MediatR;

namespace B2C.Application.Auth.Commands.Login
{

    /// <summary>
    /// Вход покупателя. SessionId опциональный — если передан,
    /// при успешном логине сольём гостевую корзину с пользовательской (US-CART-03).
    /// 
    /// SessionId извлекается контроллером из заголовка X-Session-Id и передаётся в команду
    /// (а не берётся через ISessionContext в handler'е) — чтобы команда была самодостаточной
    /// для тестирования.
    /// </summary>
    public sealed record LoginCommand(
        string Email,
        string Password,
        string? SessionId) : IRequest<TokenResponseDto>;
}
