using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace B2C.Application.Auth.Commands.Logout
{
    /// <summary>
    /// Выход. Передаваем сам refresh, а не его хэш — handler сам захэширует.
    /// Это позволяет клиенту просто отдать токен из своего хранилища.
    /// </summary>
    public sealed record LogoutCommand(string RefreshToken) : IRequest;
}
