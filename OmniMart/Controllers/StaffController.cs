using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniMart.Application.Features.Staff.Commands;
using OmniMart.Application.Features.Staff.Queries;
using OmniMart.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Controllers;

[ApiController]
[Route("api/staff")]
public class StaffController : BaseController
{
    private readonly IMediator _mediator;

    public StaffController(IMediator mediator)
    {
        _mediator = mediator;
    }

    #region  

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost]
    public async Task<IActionResult> CreateStaffAsync([FromBody] RegisterStaffCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> GetAllStaff([FromQuery] GetAllStaffQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPatch("{id:guid}/salary")]
    public async Task<IActionResult> IncreaseSalary([FromRoute] Guid id, [FromBody] decimal amount, CancellationToken cancellationToken)
    {
        var command = new IncreaseStaffSalaryCommand(id, amount);
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPatch("{id:guid}/department")]
    public async Task<IActionResult> TransferDepartment([FromRoute] Guid id, [FromBody] Department newDepartment, CancellationToken cancellationToken)
    {
        var command = new TransferStaffDepartmentCommand(id, newDepartment);
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    #endregion
}