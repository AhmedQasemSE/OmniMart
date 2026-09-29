using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniMart.Application.Features.Products.Commands;
using OmniMart.Application.Features.Products.Queries;
using OmniMart.Application.Features.Products.Queries.GetProductById;
using OmniMart.Application.Features.Products.Queries.GetProducts;
using OmniMart.Application.Features.Staff.Commands;
using OmniMart.Application.Features.Vendors.Commands;
using OmniMart.Application.Features.Vendors.Queries;

namespace OmniMart.Controllers;

[ApiController]
[Route("api/vendors")]

public class VendorController : BaseController
{
    private readonly IMediator _mediator;
    public VendorController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [Authorize(Roles = "Vendor")]
    [HttpGet("profile")]
    public async Task<IActionResult> GetVendorProfile(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetVendorProfileQuery(), cancellationToken);
        return HandleResult(result);
    }
    [Authorize(Roles = "Admin,SuperAdmin")] 
    [HttpPatch("{id}/approve")]
    public async Task<IActionResult> ApproveVendor([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var command = new ApproveVendorCommand(id);
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}