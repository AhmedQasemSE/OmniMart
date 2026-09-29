using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniMart.Application.Features.Categories.Commands;
using OmniMart.Application.Features.Categories.Queries.GetAllCategories;
using OmniMart.Application.Features.Categories.Queries.GetCategoryById;

namespace OmniMart.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoryController : BaseController
{
    private readonly IMediator _mediator;
    public CategoryController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    public async Task<IActionResult> CreateCategoryAsync([FromBody] CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPut]
    public async Task<IActionResult> UpdateCategoryAsync([FromBody] UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCategoriesAsync(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllCategoriesQuery(), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCategoryByIdQuery(id), cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteCategoryCommand(id), cancellationToken);
        return HandleResult(result);
    }
    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPatch("{id}/activate")]
    public async Task<IActionResult> ActivateCategoryAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ActivateCategoryCommand(id), cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPatch("{id}/deactivate")]
    public async Task<IActionResult> DeactivateCategoryAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeactivateCategoryCommand(id), cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPatch("{id}/restore")]
    public async Task<IActionResult> RestoreCategoryAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RestoreCategoryCommand(id), cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPatch("{id}/toggle-status")]
    public async Task<IActionResult> ToggleCategoryStatusAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ToggleCategoryStatusCommand(id), cancellationToken);
        return HandleResult(result);
    }
}

