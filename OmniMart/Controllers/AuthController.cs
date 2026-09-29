using MediatR;
using Microsoft.AspNetCore.Mvc;
using OmniMart.API.Extensions;
using OmniMart.Application.Features.Auth.Command;
using OmniMart.Application.Features.Auth.Commands.ForgotPassword;
using OmniMart.Application.Features.Customers.Commands;
using OmniMart.Application.Features.Vendors.Commands;
namespace OmniMart.Controllers;

[ApiController]
[Route("api/Auth")] 
public class AuthController : BaseController
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("customer")]
    public async Task<IActionResult> RegisterCustomerAsync([FromBody] RegisterCustomerCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
    [HttpPost("vendor")]
    public async Task<IActionResult> RegisterVendorAsync([FromBody] RegisterVendorCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            Response.SetAuthCookies(result.Value.Token, result.Value.RefreshToken);
        }

        return HandleResult(result);
    }
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue("RefreshToken", out var refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new { Error = "Session expired. Please log in again." });
        }

        var command = new RefreshTokenCommand(refreshToken);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            Response.SetAuthCookies(result.Value.Token, result.Value.RefreshToken);
        }

        return HandleResult(result);
    }

    [HttpPost("forgotPassword")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);

    }
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var result =await _mediator.Send(command,cancellationToken);
        return HandleResult(result);
    }
}
