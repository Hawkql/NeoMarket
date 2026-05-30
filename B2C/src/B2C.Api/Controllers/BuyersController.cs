using B2C.Api.Contracts;
using B2C.Application.Addresses.Commands.CreateAddress;
using B2C.Application.Addresses.Commands.DeleteAddress;
using B2C.Application.Addresses.Commands.SetDefaultAddress;
using B2C.Application.Addresses.Commands.UpdateAddress;
using B2C.Application.Addresses.Queries.GetMyAddress;
using B2C.Application.Addresses.Queries.ListMyAddresses;
using B2C.Application.Auth.Commands.ChangePassword;
using B2C.Application.Auth.Commands.DeleteMyAccount;
using B2C.Application.Auth.Commands.UpdateMyProfile;
using B2C.Application.Auth.Queries.GetMyProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2C.Api.Controllers
{
    [ApiController]
    [Route("api/v1/buyers")]
    [Authorize]   // все endpoints — только для аутентифицированного покупателя
    public sealed class BuyersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BuyersController(IMediator mediator) => _mediator = mediator;

        // ===================== Профиль =====================

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile(CancellationToken ct)
        {
            var profile = await _mediator.Send(new GetMyProfileQuery(), ct);
            return Ok(profile);
        }

        [HttpPatch("me")]
        public async Task<IActionResult> UpdateMyProfile(
            [FromBody] UpdateProfileRequest request, CancellationToken ct)
        {
            await _mediator.Send(new UpdateMyProfileCommand(
                request.FirstName,
                request.LastName,
                request.Phone), ct);

            return NoContent();
        }

        [HttpPost("me/change-password")]
        public async Task<IActionResult> ChangePassword(
             [FromBody] ChangePasswordRequest request, CancellationToken ct)
        {
            await _mediator.Send(new ChangePasswordCommand(request.NewPassword), ct);
            return NoContent();
        }

        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMyAccount(CancellationToken ct)
        {
            await _mediator.Send(new DeleteMyAccountCommand(), ct);
            return NoContent();
        }

        // ===================== Адреса =====================

        [HttpGet("me/addresses")]
        public async Task<IActionResult> ListAddresses(CancellationToken ct)
        {
            var addresses = await _mediator.Send(new ListMyAddressesQuery(), ct);
            return Ok(addresses);
        }

        [HttpGet("me/addresses/{addressId:guid}")]
        public async Task<IActionResult> GetAddress(Guid addressId, CancellationToken ct)
        {
            var address = await _mediator.Send(new GetMyAddressQuery(addressId), ct);
            return Ok(address);
        }

        [HttpPost("me/addresses")]
        public async Task<IActionResult> CreateAddress(
            [FromBody] CreateAddressRequest request, CancellationToken ct)
        {
            var created = await _mediator.Send(new CreateAddressCommand(
                request.Country,
                request.City,
                request.Street,
                request.House,
                request.Apartment,
                request.PostalCode,
                request.IsDefault), ct);

            return CreatedAtAction(nameof(GetAddress), new { addressId = created.Id }, created);
        }

        [HttpPut("me/addresses/{addressId:guid}")]
        public async Task<IActionResult> UpdateAddress(
            Guid addressId, [FromBody] UpdateAddressRequest request, CancellationToken ct)
        {
            await _mediator.Send(new UpdateAddressCommand(
                addressId,
                request.Country,
                request.City,
                request.Street,
                request.House,
                request.Apartment,
                request.PostalCode), ct);

            return NoContent();
        }

        [HttpPost("me/addresses/{addressId:guid}/set-default")]
        public async Task<IActionResult> SetDefaultAddress(Guid addressId, CancellationToken ct)
        {
            await _mediator.Send(new SetDefaultAddressCommand(addressId), ct);
            return NoContent();
        }

        [HttpDelete("me/addresses/{addressId:guid}")]
        public async Task<IActionResult> DeleteAddress(Guid addressId, CancellationToken ct)
        {
            await _mediator.Send(new DeleteAddressCommand(addressId), ct);
            return NoContent();
        }
    }
}
