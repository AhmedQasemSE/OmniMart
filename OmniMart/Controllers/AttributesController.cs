using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniMart.Application.Features.Attributes.Commands;
using OmniMart.Application.Features.Attributes.Queries;

namespace OmniMart.Controllers;

[ApiController]
[Route("api/attributes")]
[Authorize(Roles = "Admin,SuperAdmin")] 
public class AttributesController : BaseController
{
    private readonly IMediator _mediator;

    public AttributesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAttribute([FromBody] CreateProductAttributeCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAttribute([FromRoute] Guid id, [FromBody] UpdateProductAttributeCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { Id = id }, cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAttribute([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteProductAttributeCommand(id), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAttributes([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAllProductAttributesQuery(pageNumber, pageSize), cancellationToken);
        return HandleResult(result);
    }
}