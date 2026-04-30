using B2B.Application.Invoices.commands.AcceptInvoices;
using B2B.Application.Invoices.commands.CreateInvoices;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace B2B.Api.Controllers
{

    [ApiController]
    [Route("api/v1/invoices")]
    public class InvoicesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public InvoicesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateInvoiceRequest req,
            CancellationToken ct)
        {
            var command = new CreateInvoiceCommand(
                req.SellerId,
                req.Number,
                req.Lines.Select( l => new InvoiceLineDto(l.SkuId, l.Quantity, l.Cost)).ToList());

            var id = await _mediator.Send(command, ct);

            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }

        [HttpPost("accept")]
        public async Task<IActionResult> Accept(
            [FromBody] AcceptInvoiceRequest req,
            CancellationToken ct)
        {
            await _mediator.Send(new AcceptInvoiceCommand(req.InvoiceId), ct);
            return NoContent();
        }

        [HttpGet("{id:guid}")]
        public IActionResult GetById(Guid id)
        {
            // Заглушка — добавишь, когда понадобится Query на чтение накладной
            return NotFound();
        }
    }

    public record CreateInvoiceRequest(
        Guid SellerId,
        string Number,
        List<InvoiceLineRequest> Lines);

    public record InvoiceLineRequest(Guid SkuId, int Quantity, decimal Cost);

    public record AcceptInvoiceRequest(Guid InvoiceId);
}
