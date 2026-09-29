using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Users.Commands;

public record SuspendUserCommand(Guid TargetUserId, string Reason) : IRequest<Result<bool>>;

public class SuspendUserCommandHandler : IRequestHandler<SuspendUserCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SuspendUserCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public SuspendUserCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<SuspendUserCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(SuspendUserCommand request, CancellationToken cancellationToken)
    {
        string currentRole = _currentUserService.Role ?? "";

        if (currentRole != SystemRole.Admin.ToString() && currentRole != SystemRole.SuperAdmin.ToString())
        {
            _logger.LogWarning("Unauthorized suspension attempt by user {UserId} with role {Role}.", _currentUserService.UserId, currentRole);
            return Result<bool>.Failure("Only Administrators can suspend users.", ErrorType.Unauthorized);
        }

        var targetUser = await _unitOfWork.Users.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (targetUser == null)
        {
            _logger.LogWarning("Suspension attempt failed: User {TargetUserId} not found.", request.TargetUserId);
            return Result<bool>.Failure("User not found.", ErrorType.NotFound);
        }

        if (targetUser.Id.ToString() == _currentUserService.UserId)
        {
            _logger.LogWarning("User {UserId} attempted to suspend their own account.", _currentUserService.UserId);
            return Result<bool>.Failure("You cannot suspend your own account.", ErrorType.Validation);
        }

        if (targetUser.Role == SystemRole.SuperAdmin)
        {
            _logger.LogWarning("Unauthorized suspension attempt on SuperAdmin account by user {UserId} with role {Role}.", _currentUserService.UserId, currentRole);
            return Result<bool>.Failure("The SuperAdmin account cannot be suspended.", ErrorType.Unauthorized);
        }

        if (currentRole == SystemRole.Admin.ToString() && targetUser.Role == SystemRole.Admin)
        {
            _logger.LogWarning("Unauthorized suspension attempt on another Admin account by user {UserId} with role {Role}.", _currentUserService.UserId, currentRole);
            return Result<bool>.Failure("You do not have permission to suspend another Administrator.", ErrorType.Unauthorized);
        }

        if (!targetUser.IsActive)
        {
            _logger.LogWarning("Suspension attempt failed: User {TargetUserId} is already suspended or inactive.", request.TargetUserId);   
            return Result<bool>.Failure("This user is already suspended or inactive.", ErrorType.Conflict);
        }

        targetUser.Suspend(request.Reason);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {TargetUserId} (Role: {TargetRole}) was suspended by {ExecutorRole} {ExecutorId}. Reason: {Reason}",
            targetUser.Id, targetUser.Role, currentRole, _currentUserService.UserId, request.Reason);

        return Result<bool>.Success(true);
    }
}

public class SuspendUserCommandValidator : AbstractValidator<SuspendUserCommand>
{
    public SuspendUserCommandValidator()
    {
        RuleFor(x => x.TargetUserId).NotEmpty().WithMessage("Target User ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Suspension reason is required.")
            .MinimumLength(5).WithMessage("Please provide a clear and meaningful reason for the suspension.");
    }
}