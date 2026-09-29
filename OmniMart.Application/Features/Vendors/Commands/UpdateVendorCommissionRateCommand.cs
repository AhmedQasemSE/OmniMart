using FluentValidation;
using MediatR;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Products.Queries.GetAdminProducts;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Vendors.Commands;

public record UpdateVendorCommissionRateCommand(Guid VendorId, decimal NewRate, byte[] RowVersion) : IRequest<Result<bool>>;

public class UpdateVendorCommissionRateCommandHandler : IRequestHandler<UpdateVendorCommissionRateCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateVendorCommissionRateCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(UpdateVendorCommissionRateCommand request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString)||!Guid.TryParse(userIdString,out Guid userGuid))
            return Result<bool>.Failure("Unauthorized: Invalid user token.", ErrorType.Unauthorized);

        Guid? adminId = await _unitOfWork.StaffProfiles.GetStaffIdByUserIdAsync(userGuid , cancellationToken);
        if (adminId == null)
            return Result<bool>.Failure("Admin profile not found.", ErrorType.NotFound);

        var vendor = await _unitOfWork.VendorProfiles.GetByIdAsync(request.VendorId);
        if(vendor==null)
            return Result<bool>.Failure("Vendor profile not found.", ErrorType.NotFound);
        vendor.UpdateCommissionRate(request.NewRate);
        _unitOfWork.VendorProfiles.SetOriginalRowVersion(vendor, request.RowVersion); 

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            return Result<bool>.Failure("Data discrepancy: The merchant's account has been modified by another process. Please update.", ErrorType.Conflict);
        }
    }
}

public class UpdateVendorCommissionRateCommandValidator : AbstractValidator<UpdateVendorCommissionRateCommand>
{
    public UpdateVendorCommissionRateCommandValidator()
    {
        RuleFor(x => x.VendorId)
            .NotEmpty().WithMessage("Vendor ID cannot be empty.");

        RuleFor(x => x.NewRate)
            .GreaterThanOrEqualTo(0).WithMessage("Commission rate cannot be negative.")
            .LessThanOrEqualTo(100).WithMessage("Commission rate cannot exceed 100%.");
    }
}