using B2B.Api.Contracts;
using B2B.Application.Common.Abstractions;
using B2B.Application.Invoices.Commands.AcceptInvoice;
using B2B.Application.Invoices.Commands.CreateInvoice;
using B2B.Application.Invoices.Commands.DeleteInvoice;
using B2B.Application.Invoices.Queries.GetInvoiceById;
using B2B.Application.Invoices.Queries.ListInvoices;
using B2B.Domain.Invoices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2B.Api.Controllers
{
    [ApiController]
    [Route("api/v1/invoices")]
    [Authorize(Policy = "SellerOnly")]
    public sealed class InvoicesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUser;

        public InvoicesController(IMediator mediator, ICurrentUserService currentUser)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateInvoiceRequest request, CancellationToken ct)
        {
            var command = new CreateInvoiceCommand(
                SellerId: _currentUser.SellerId,
                Items: request.Items);

            var result = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] InvoiceStatus? status,
            [FromQuery] int limit = 20,
            [FromQuery] int offset = 0,
            CancellationToken ct = default)
        {
            var query = new ListInvoicesQuery(
                _currentUser.SellerId, status, limit, offset);
            return Ok(await _mediator.Send(query, ct));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var result = await _mediator.Send(
                new GetInvoiceByIdQuery(id, _currentUser.SellerId), ct);
            return Ok(result);
        }

        [HttpPost("{id:guid}/accept")]
        public async Task<IActionResult> Accept(
            Guid id, [FromBody] AcceptInvoiceRequest? request, CancellationToken ct)
        {
            var command = new AcceptInvoiceCommand(
                InvoiceId: id,
                AcceptedBy: _currentUser.SellerId,           
                AcceptedItems: request?.AcceptedItems);       

            return Ok(await _mediator.Send(command, ct));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteInvoiceCommand(id, _currentUser.SellerId), ct);
            return NoContent();
        }
    }
}
