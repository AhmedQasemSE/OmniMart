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

public record UpdateVendorProfileCommand(string StoreName, string CommercialRegisterNumber) : IRequest<Result<bool>>;

public class UpdateVendorProfileCommandHandler : IRequestHandler<UpdateVendorProfileCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateVendorProfileCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public UpdateVendorProfileCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateVendorProfileCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(UpdateVendorProfileCommand request, CancellationToken cancellationToken)
    {
        string currentRole = _currentUserService.Role ?? "";
        string? userIdString = _currentUserService.UserId;

        if (currentRole != SystemRole.Vendor.ToString() || !Guid.TryParse(userIdString, out Guid userGuid))
        {
            return Result<bool>.Failure("Only vendors can update their store profiles.", ErrorType.Unauthorized);
        }

        var vendorProfile = await _unitOfWork.VendorProfiles.GetAsync(v => v.UserId == userGuid, cancellationToken);
        if (vendorProfile == null)
        {
            return Result<bool>.Failure("Vendor profile not found.", ErrorType.NotFound);
        }
        try {
            vendorProfile.UpdateProfile(request.StoreName, request.CommercialRegisterNumber);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Vendor {VendorId} successfully updated their profile.", vendorProfile.Id);

            return Result<bool>.Success(true);
        } catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating vendor profile for user {UserId}.", userIdString);
            return Result<bool>.Failure("An error occurred while updating the vendor profile.", ErrorType.Conflict);
        }   
    }
}

public class UpdateVendorProfileCommandValidator : AbstractValidator<UpdateVendorProfileCommand>
{
    public UpdateVendorProfileCommandValidator()
    {
        RuleFor(x => x.StoreName)
            .NotEmpty().WithMessage("Store name is required.")
            .MaximumLength(100).WithMessage("Store name cannot exceed 100 characters.");

        RuleFor(x => x.CommercialRegisterNumber)
            .NotEmpty().WithMessage("Commercial Register Number is required.")
            .Matches(@"^\d+$").WithMessage("Commercial Register Number must contain only numbers.");
    }
}