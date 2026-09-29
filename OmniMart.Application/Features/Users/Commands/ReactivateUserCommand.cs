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

public record ReactivateUserCommand(Guid TargetUserId) : IRequest<Result<bool>>;

public class ReactivateUserCommandHandler : IRequestHandler<ReactivateUserCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReactivateUserCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public ReactivateUserCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<ReactivateUserCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(ReactivateUserCommand request, CancellationToken cancellationToken)
    {
        string currentRole = _currentUserService.Role ?? "";

        if (currentRole != SystemRole.Admin.ToString() && currentRole != SystemRole.SuperAdmin.ToString())
        {
            _logger.LogWarning("Unauthorized attempt to reactivate user {TargetUserId} by {ExecutorRole} {ExecutorId}.",
                request.TargetUserId, currentRole, _currentUserService.UserId);
            return Result<bool>.Failure("Only Administrators can reactivate users.", ErrorType.Unauthorized);
        }

        var targetUser = await _unitOfWork.Users.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (targetUser == null)
        {
            _logger.LogWarning("User {TargetUserId} not found for reactivation by {ExecutorRole} {ExecutorId}.",
                request.TargetUserId, currentRole, _currentUserService.UserId);
            return Result<bool>.Failure("User not found.", ErrorType.NotFound);
        }

        if (targetUser.IsActive)
        {
            return Result<bool>.Failure("This user is already active.", ErrorType.Conflict);
        }

        targetUser.Reactivate();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {TargetUserId} was reactivated by {ExecutorRole} {ExecutorId}.",
            targetUser.Id, currentRole, _currentUserService.UserId);

        return Result<bool>.Success(true);
    }
}

public class ReactivateUserCommandValidator : AbstractValidator<ReactivateUserCommand>
{
    public ReactivateUserCommandValidator()
    {
        RuleFor(x => x.TargetUserId).NotEmpty().WithMessage("Target User ID is required.");
    }
}