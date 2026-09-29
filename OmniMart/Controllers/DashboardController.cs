using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniMart.Application.Features.Dashboard.Queries;

namespace OmniMart.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : BaseController
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetAdminDashboardStats(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAdminDashboardStatsQuery(), cancellationToken);
        return HandleResult(result);
    }
}