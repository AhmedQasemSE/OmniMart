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

namespace OmniMart.Application.Features.Vendors.Commands;

public record ApproveVendorCommand(Guid VendorId) : IRequest<Result<bool>>;

public class ApproveVendorCommandHandler : IRequestHandler<ApproveVendorCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApproveVendorCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public ApproveVendorCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<ApproveVendorCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(ApproveVendorCommand request, CancellationToken cancellationToken)
    {
        string currentRole = _currentUserService.Role ?? "";

        if (currentRole != SystemRole.Admin.ToString() && currentRole != SystemRole.SuperAdmin.ToString())
        {
            _logger.LogWarning("Unauthorized attempt to approve vendor by user {UserId}.", _currentUserService.UserId);
            return Result<bool>.Failure("Only Administrators can approve vendor accounts.", ErrorType.Unauthorized);
        }

        var vendorProfile = await _unitOfWork.VendorProfiles.GetByIdAsync(request.VendorId, cancellationToken);
        if (vendorProfile == null)
        {
            return Result<bool>.Failure("Vendor profile not found.", ErrorType.NotFound);
        }

        try
        {
            vendorProfile.ApproveAccount();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Vendor {VendorId} ({StoreName}) was successfully approved by Administrator {AdminId}.",
                vendorProfile.Id, vendorProfile.StoreName, _currentUserService.UserId);

            return Result<bool>.Success(true);
        }
        catch (InvalidOperationException ex)
        {
            return Result<bool>.Failure(ex.Message, ErrorType.Conflict);
        }
    }
}

public class ApproveVendorCommandValidator : AbstractValidator<ApproveVendorCommand>
{
    public ApproveVendorCommandValidator()
    {
        RuleFor(x => x.VendorId).NotEmpty().WithMessage("Vendor ID is required.");
    }
}