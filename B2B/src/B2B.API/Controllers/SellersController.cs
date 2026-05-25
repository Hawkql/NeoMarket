using B2B.Api.Contracts;
using B2B.Application.Auth.Commands.DeleteMyAccount;
using B2B.Application.Auth.Commands.UpdateMyProfile;
using B2B.Application.Auth.Queries.GetMyProfile;
using B2B.Application.Common.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2B.Api.Controllers
{
    [ApiController]
    [Route("api/v1/sellers")]
    [Authorize(Policy = "SellerOnly")]
    public sealed class SellersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public SellersController(IMediator mediator, ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe(CancellationToken ct)
        {
            var result = await _mediator.Send(
                new GetMyProfileQuery(_currentUser.SellerId), ct);
            return Ok(result);
        }

        [HttpPatch("me")]
        public async Task<IActionResult> UpdateMe(
            [FromBody] UpdateSellerRequest request, CancellationToken ct)
        {
            var command = new UpdateMyProfileCommand(
                SellerId: _currentUser.SellerId,   // из JWT
                FirstName: request.FirstName,
                LastName: request.LastName,
                MiddleName: request.MiddleName,
                CompanyName: request.CompanyName,
                Phone: request.Phone);

            return Ok(await _mediator.Send(command, ct));
        }

        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMe(CancellationToken ct)
        {
            await _mediator.Send(new DeleteMyAccountCommand(_currentUser.SellerId), ct);
            return NoContent();
        }
    }
}
