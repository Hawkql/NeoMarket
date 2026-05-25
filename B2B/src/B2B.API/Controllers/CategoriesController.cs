using B2B.Api.Contracts;
using B2B.Application.Categories.Commands.CreateCategory;
using B2B.Application.Categories.Commands.DeleteCategory;
using B2B.Application.Categories.Commands.UpdateCategory;
using B2B.Application.Categories.Queries.GetBreadcrumbs;
using B2B.Application.Categories.Queries.GetCategoriesTree;
using B2B.Application.Categories.Queries.GetCategory;
using B2B.Application.Categories.Queries.ListCategories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2B.Api.Controllers
{
    [ApiController]
    [Route("api/v1/categories")]
    public sealed class CategoriesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CategoriesController(IMediator mediator) => _mediator = mediator;

        // ───── READ (открытые) ─────

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> List(
            [FromQuery(Name = "parent_id")] Guid? parentId,
            [FromQuery(Name = "only_root")] bool onlyRoot = false,
            CancellationToken ct = default)
        {
            var result = await _mediator.Send(
                new ListCategoriesQuery(parentId, onlyRoot), ct);
            return Ok(result);
        }

        [HttpGet("tree")]
        [AllowAnonymous]
        public async Task<IActionResult> Tree(CancellationToken ct)
        {
            return Ok(await _mediator.Send(new GetCategoriesTreeQuery(), ct));
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            return Ok(await _mediator.Send(new GetCategoryQuery(id), ct));
        }

        [HttpGet("{id:guid}/breadcrumbs")]
        [AllowAnonymous]
        public async Task<IActionResult> Breadcrumbs(Guid id, CancellationToken ct)
        {
            return Ok(await _mediator.Send(new GetBreadcrumbsQuery(id), ct));
        }

        // ───── WRITE (только админ) ─────

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(
            [FromBody] CreateCategoryRequest request, CancellationToken ct)
        {
            var command = new CreateCategoryCommand(request.Name, request.ParentId);
            var result = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPatch("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(
            Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken ct)
        {
            var command = new UpdateCategoryCommand(
                CategoryId: id,
                Name: request.Name,
                ParentId: request.ParentId,
                ParentIdSpecified: request.ParentIdSpecified,
                IsActive: request.IsActive);

            return Ok(await _mediator.Send(command, ct));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await _mediator.Send(new DeleteCategoryCommand(id), ct);
            return NoContent();
        }
    }
}
