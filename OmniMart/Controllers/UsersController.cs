using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniMart.Application.Features.Users.Commands;

namespace OmniMart.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class UsersController : BaseController
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPatch("{id:guid}/change-role")]
    public async Task<IActionResult> ChangeUserRole([FromRoute] Guid id, [FromBody] ChangeUserRoleCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { TargetUserId = id }, cancellationToken);
        return HandleResult(result);
    }

    [HttpPatch("{id:guid}/suspend")]
    public async Task<IActionResult> SuspendUser([FromRoute] Guid id, [FromBody] SuspendUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { TargetUserId = id }, cancellationToken);
        return HandleResult(result);
    }

    [HttpPatch("{id:guid}/reactivate")]
    public async Task<IActionResult> ReactivateUser([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ReactivateUserCommand(id), cancellationToken);
        return HandleResult(result);
    }
}