using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniMart.API.Filters;
using OmniMart.Application.Features.Orders.Commands;
using OmniMart.Application.Features.Orders.Queries;
using OmniMart.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Controllers;

[ApiController]
[Route("api/orders")]
public class OrderController : BaseController
{
    private readonly IMediator _mediator;

    public OrderController(IMediator mediator)
    {
        _mediator = mediator;
    }

    #region  (Customer Orders)

    [Authorize(Roles = "Customer")]
    [HttpGet]
    public async Task<IActionResult> GetCustomerOrders([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetCustomerOrdersQuery(pageNumber, pageSize), cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrderDetails([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetCustomerOrderDetailsQuery(id), cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Customer")]
    [Idempotent]
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    #endregion

    #region  (Vendor Orders)

    [Authorize(Roles = "Vendor")]
    [HttpGet("vendor-orders")]
    public async Task<IActionResult> GetVendorOrders([FromQuery] OrderStatus? status, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetVendorOrdersQuery(status, pageNumber, pageSize), cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Vendor")]
    [HttpGet("vendor-orders/{id:guid}")]
    public async Task<IActionResult> GetVendorOrderDetails([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetVendorOrderDetailsQuery(id), cancellationToken);
        return HandleResult(result);
    }

    #endregion

    #region  (Admin Orders)

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet("admin-orders")]
    public async Task<IActionResult> GetAdminOrders([FromQuery] GetAdminOrdersQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    #endregion

    #region  (Order Processing)

    [HttpPatch("{id:guid}/process")]
    [Authorize(Roles = "Admin,Vendor")]
    public async Task<IActionResult> ProcessOrder([FromRoute] Guid id, [FromBody] byte[] rowVersion, CancellationToken cancellationToken)
    {
        var command = new ProcessOrderCommand(id, rowVersion);
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPatch("{id:guid}/ship")]
    [Authorize(Roles = "Admin,Vendor")]
    public async Task<IActionResult> ShipOrder([FromRoute] Guid id, [FromBody] byte[] rowVersion, CancellationToken cancellationToken)
    {
        var command = new ShipOrderCommand(id, rowVersion);
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPatch("{id:guid}/deliver")]
    [Authorize(Roles = "Admin,Vendor")]
    public async Task<IActionResult> DeliverOrder([FromRoute] Guid id, [FromBody] byte[] rowVersion, CancellationToken cancellationToken)
    {
        var command = new DeliverOrderCommand(id, rowVersion);
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Roles = "Admin,Vendor,Customer")]
    public async Task<IActionResult> CancelOrder([FromRoute] Guid id, [FromBody] byte[] rowVersion, CancellationToken cancellationToken)
    {
        var command = new CancelOrderCommand(id, rowVersion);
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPatch("{id:guid}/refund")]
    [Authorize(Roles = "Admin,Vendor")]
    public async Task<IActionResult> RefundOrder([FromRoute] Guid id, [FromBody] byte[] rowVersion, CancellationToken cancellationToken)
    {
        var command = new RefundOrderCommand(id, rowVersion);
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    #endregion
}