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

public record IncreaseStaffSalaryCommand(Guid StaffUserId, decimal Amount) : IRequest<Result<bool>>;

public class IncreaseStaffSalaryCommandHandler : IRequestHandler<IncreaseStaffSalaryCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IncreaseStaffSalaryCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public IncreaseStaffSalaryCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<IncreaseStaffSalaryCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(IncreaseStaffSalaryCommand request, CancellationToken cancellationToken)
    {
        string currentRole = _currentUserService.Role ?? "";

        if (currentRole != SystemRole.Admin.ToString() && currentRole != SystemRole.SuperAdmin.ToString())
        {
            _logger.LogWarning("Security Alert: User {UserId} attempted an unauthorized salary increase.", _currentUserService.UserId);
            return Result<bool>.Failure("Only Administrators can modify staff salaries.", ErrorType.Unauthorized);
        }

        var staffProfile = await _unitOfWork.StaffProfiles.GetAsync(s => s.UserId == request.StaffUserId, cancellationToken);

        if (staffProfile == null)
        {
            return Result<bool>.Failure("Staff profile not found.", ErrorType.NotFound);
        }
        if (request.StaffUserId.ToString() == _currentUserService.UserId)
        {
            _logger.LogWarning("Fraud Prevention Alert: User {UserId} attempted to increase their own salary.", _currentUserService.UserId);
            return Result<bool>.Failure("Segregation of Duties violation: You cannot modify your own salary.", ErrorType.Conflict);
        }
        var oldSalary = staffProfile.Salary;

        try
        {
            staffProfile.IncreaseSalary(request.Amount);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("FINANCIAL AUDIT: Salary for Staff {StaffNumber} increased by {Amount:C}. Old Salary: {OldSalary:C}, New Salary: {NewSalary:C}. Executed by {ExecutorId}.",
                staffProfile.StaffNumber, request.Amount, oldSalary, staffProfile.Salary, _currentUserService.UserId);

            return Result<bool>.Success(true);
        }
        catch (ArgumentException ex)
        {
            return Result<bool>.Failure(ex.Message, ErrorType.Validation);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<bool>.Failure("The staff profile was modified by another user. Please refresh.", ErrorType.Conflict);
        }
    }
}

public class IncreaseStaffSalaryCommandValidator : AbstractValidator<IncreaseStaffSalaryCommand>
{
    public IncreaseStaffSalaryCommandValidator()
    {
        RuleFor(x => x.StaffUserId).NotEmpty().WithMessage("Staff User ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Increase amount must be greater than zero.")
            .LessThanOrEqualTo(10000).WithMessage("Increase amount exceeds the maximum allowable limit per transaction (10,000)."); 
    }
}