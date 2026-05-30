using System.Net.NetworkInformation;
using B2C.Api.Contracts;
using B2C.Application.Auth.Commands.Login;
using B2C.Application.Auth.Commands.Logout;
using B2C.Application.Auth.Commands.Refresh;
using B2C.Application.Auth.Commands.Register;
using B2C.Application.Common.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2C.Api.Controllers
{



    [ApiController]
    [Route("api/v1/auth")]
    [AllowAnonymous]
    public sealed class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ISessionContext _session;

        public AuthController(IMediator mediator, ISessionContext session)
        {
            _mediator = mediator;
            _session = session;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest request, CancellationToken ct)
        {
            var result = await _mediator.Send(new BuyerRegisterCommand(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                request.Phone), ct);

            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request, CancellationToken ct)
        {
            // SessionId из заголовка X-Session-Id — для merge гостевой корзины (US-CART-03).
            var result = await _mediator.Send(new LoginCommand(
                request.Email,
                request.Password,
                _session.SessionId), ct);

            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(
            [FromBody] RefreshRequest request, CancellationToken ct)
        {
            var result = await _mediator.Send(new RefreshTokenCommand(request.RefreshToken), ct);
            return Ok(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(
            [FromBody] LogoutRequest request, CancellationToken ct)
        {
            await _mediator.Send(new LogoutCommand(request.RefreshToken), ct);
            return NoContent();
        }
    }

}
