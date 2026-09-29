using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Staff.Commands;

public record TransferStaffDepartmentCommand(Guid StaffUserId, Department NewDepartment) : IRequest<Result<bool>>;

public class TransferStaffDepartmentCommandHandler : IRequestHandler<TransferStaffDepartmentCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransferStaffDepartmentCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public TransferStaffDepartmentCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<TransferStaffDepartmentCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(TransferStaffDepartmentCommand request, CancellationToken cancellationToken)
    {
        string currentRole = _currentUserService.Role ?? "";

        if (currentRole != SystemRole.Admin.ToString() && currentRole != SystemRole.SuperAdmin.ToString())
        {
            _logger.LogWarning("Security Alert: User {UserId} attempted an unauthorized staff transfer.", _currentUserService.UserId);
            return Result<bool>.Failure("Only Administrators can transfer staff between departments.", ErrorType.Unauthorized);
        }

        var staffProfile = await _unitOfWork.StaffProfiles.GetAsync(s => s.UserId == request.StaffUserId, cancellationToken);

        if (staffProfile == null)
        {
            return Result<bool>.Failure("Staff profile not found. Ensure the user is actually a staff member.", ErrorType.NotFound);
        }

        try
        {
            var oldDepartment = staffProfile.DepartmentRole;

            staffProfile.TransferDepartment(request.NewDepartment);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Staff {StaffNumber} successfully transferred from {OldDept} to {NewDept} by {ExecutorId}.",
                staffProfile.StaffNumber, oldDepartment, request.NewDepartment, _currentUserService.UserId);

            return Result<bool>.Success(true);
        }
        catch (InvalidOperationException ex) 
        {
            _logger.LogError(ex, "Validation error during staff transfer for Staff {StaffNumber}.", staffProfile.StaffNumber);
            return Result<bool>.Failure(ex.Message, ErrorType.Validation);
        }
        catch (DbUpdateConcurrencyException) 
        {
            _logger.LogError("Concurrency error during staff transfer for Staff {StaffNumber}.", staffProfile.StaffNumber);
            return Result<bool>.Failure("This employee's data has just been modified by someone else. Please refresh the page..", ErrorType.Conflict);
        }
    }
}

public class TransferStaffDepartmentCommandValidator : AbstractValidator<TransferStaffDepartmentCommand>
{
    public TransferStaffDepartmentCommandValidator()
    {
        RuleFor(x => x.StaffUserId).NotEmpty().WithMessage("Staff User ID is required.");

        RuleFor(x => x.NewDepartment).IsInEnum().WithMessage("Invalid department specified.");
    }
}