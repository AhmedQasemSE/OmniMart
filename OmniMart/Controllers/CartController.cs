using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniMart.Application.Features.Cart.Commands;
using OmniMart.Application.Features.Cart.Queries;

namespace OmniMart.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize(Roles = "Customer")]
public class CartController : BaseController
{
    private readonly IMediator _mediator;
    public CartController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("items/{productVariantId}")]
    public async Task<IActionResult> UpdateCartItemQuantity([FromRoute] Guid productVariantId, [FromBody] int newQuantity, CancellationToken cancellationToken)
    {
        var command = new UpdateCartItemQuantityCommand(productVariantId, newQuantity);
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("items/{productVariantId}")]
    public async Task<IActionResult> RemoveCartItem([FromRoute] Guid productVariantId, CancellationToken cancellationToken)
    {
        var command = new RemoveCartItemCommand(productVariantId);
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearCart(CancellationToken cancellationToken)
    {
        var command = new ClearCartCommand();
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveCart(CancellationToken cancellationToken)
    {
        var query = new GetActiveCartQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }
}