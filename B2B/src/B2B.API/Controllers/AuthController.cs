using B2B.Application.Auth.Commands.Login;
using B2B.Application.Auth.Commands.Logout;
using B2B.Application.Auth.Commands.Refresh;
using B2B.Application.Auth.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace B2B.Api.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    [AllowAnonymous]   // auth-эндпоинты открыты (токенов ещё нет)
    public sealed class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator) => _mediator = mediator;

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] B2B.Api.Contracts.RegisterRequest request, CancellationToken ct)
        {
            var command = new RegisterCommand(
                request.Email, request.Password, request.FirstName, request.LastName,
                request.MiddleName, request.CompanyName, request.Inn, request.Phone);

            var result = await _mediator.Send(command, ct);
            return StatusCode(StatusCodes.Status201Created, result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request, CancellationToken ct)
        {
            var result = await _mediator.Send(
                new LoginCommand(request.Email, request.Password), ct);
            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(
            [FromBody] RefreshRequest request, CancellationToken ct)
        {
            var result = await _mediator.Send(
                new RefreshCommand(request.RefreshToken), ct);
            return Ok(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(
            [FromBody] RefreshRequest request, CancellationToken ct)
        {
            await _mediator.Send(new LogoutCommand(request.RefreshToken), ct);
            return NoContent();
        }
    }
}
