using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniMart.Application.Features.CustomerProfiles.Commands;
using OmniMart.Application.Features.Customers.Commands;
using OmniMart.Application.Features.Customers.Queries;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = "Customer")]
public class CustomerController : BaseController
{
    private readonly IMediator _mediator;

    public CustomerController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetCustomerProfile(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetCustomerProfileQuery(), cancellationToken);
        return HandleResult(result);
    }

    [HttpPatch("redeem-points")]
    public async Task<IActionResult> RedeemLoyaltyPoints([FromBody] RedeemLoyaltyPointsCommand command, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("addresses")]
    public async Task<IActionResult> AddCustomerAddress([FromBody] AddCustomerAddressCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("addresses")]
    public async Task<IActionResult> GetCustomerAddresses(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCustomerAddressesQuery(), cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("addresses/{id}")]
    public async Task<IActionResult> UpdateCustomerAddress([FromRoute] Guid id, [FromBody] UpdateCustomerAddressCommand command, CancellationToken cancellationToken)
    {
        var finalCommand = command with { AddressId = id };
        var result = await _mediator.Send(finalCommand, cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("addresses/{id}")]
    public async Task<IActionResult> DeleteCustomerAddress([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteCustomerAddressCommand(id);
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}