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

public record ChangeUserRoleCommand(Guid TargetUserId, SystemRole NewRole) : IRequest<Result<bool>>;

public class ChangeUserRoleCommandHandler : IRequestHandler<ChangeUserRoleCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChangeUserRoleCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public ChangeUserRoleCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<ChangeUserRoleCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
    {
        string currentRole = _currentUserService.Role ?? "";

        if (currentRole != SystemRole.Admin.ToString() && currentRole != SystemRole.SuperAdmin.ToString())
        {
            _logger.LogWarning("Unauthorized role change attempt by user {UserId} with role {Role}.", _currentUserService.UserId, currentRole);
            return Result<bool>.Failure("Only Administrators can change user roles.", ErrorType.Unauthorized);
        }

        var targetUser = await _unitOfWork.Users.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (targetUser == null)
        {
            _logger.LogWarning("User not found for role change attempt by user {UserId} with role {Role}.", _currentUserService.UserId, currentRole);   
            return Result<bool>.Failure("User not found.", ErrorType.NotFound);
        }

         if (targetUser.Id.ToString() == _currentUserService.UserId)
        {
            _logger.LogWarning("User {UserId} attempted to change their own role.", _currentUserService.UserId);
            return Result<bool>.Failure("You cannot change your own role.", ErrorType.Validation);
        }

        if (targetUser.Role == SystemRole.SuperAdmin)
        {
            _logger.LogWarning("User {UserId} attempted to modify the SuperAdmin role.", _currentUserService.UserId);
            return Result<bool>.Failure("The SuperAdmin account cannot be modified.", ErrorType.Unauthorized);
        }

        if (currentRole == SystemRole.Admin.ToString())
        {
            if (targetUser.Role == SystemRole.Admin)
                return Result<bool>.Failure("You do not have permission to modify another Admin's role.", ErrorType.Unauthorized);

            if (request.NewRole == SystemRole.Admin || request.NewRole == SystemRole.SuperAdmin)
                return Result<bool>.Failure("You do not have permission to grant Admin or SuperAdmin roles.", ErrorType.Unauthorized);
        }

        bool isTargetUserStaff = targetUser.Role == SystemRole.Admin || targetUser.Role == SystemRole.Manager || targetUser.Role == SystemRole.SupportAgent;
        bool isNewRoleStaff = request.NewRole == SystemRole.Manager || request.NewRole == SystemRole.SupportAgent || request.NewRole == SystemRole.Admin;

        if (!isTargetUserStaff && isNewRoleStaff)
        {
            return Result<bool>.Failure("Cannot change a Customer/Vendor to Staff directly. Please use the Staff Registration process.", ErrorType.Validation);
        }

        if (request.NewRole == SystemRole.Vendor || request.NewRole == SystemRole.Customer)
        {
            return Result<bool>.Failure("Cannot revert a Staff member to Customer or Vendor using this action.", ErrorType.Validation);
        }

        targetUser.ChangeRole(request.NewRole);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} role successfully changed to {NewRole} by {ExecutorRole} {ExecutorId}.",
            targetUser.Id, request.NewRole, currentRole, _currentUserService.UserId);

        return Result<bool>.Success(true);
    }
}

public class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleCommandValidator()
    {
        RuleFor(x => x.TargetUserId).NotEmpty().WithMessage("Target User ID is required.");
        RuleFor(x => x.NewRole).IsInEnum().WithMessage("Invalid system role specified.");
    }
}