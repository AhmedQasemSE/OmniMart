using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Products.Commands;

public record RestoreProductCommand(Guid ProductId) : IRequest<Result<bool>>;

public class RestoreProductCommandHandler : IRequestHandler<RestoreProductCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RestoreProductCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public RestoreProductCommandHandler(IUnitOfWork unitOfWork, ILogger<RestoreProductCommandHandler> logger, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(RestoreProductCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling RestoreProductCommand for Product ID: {ProductId}", request.ProductId);
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
        {
            _logger.LogWarning("Unauthorized access attempt to restore product with ID: {ProductId}. Invalid or missing user token.", request.ProductId);
            return Result<bool>.Failure("Unauthorized: Invalid user token.", ErrorType.Unauthorized);
        }

        Guid? vendorId = await _unitOfWork.VendorProfiles.GetVendorIdByUserIdAsync(userGuid, cancellationToken);
        if (vendorId is null)
        {
            _logger.LogWarning("Vendor profile not found for user ID: {UserId}. Cannot restore product with ID: {ProductId}.", userGuid, request.ProductId);
            return Result<bool>.Failure("Vendor profile not found.", ErrorType.NotFound);
        }
        var product = await _unitOfWork.Products.GetDeletedProductWithVariantsAsync(request.ProductId, cancellationToken);

        if (product == null)
        {
            _logger.LogWarning("Deleted Product with ID: {ProductId} not found. It might be permanently deleted or it is already active.", request.ProductId);
            return Result<bool>.Failure("Deleted product not found.", ErrorType.NotFound);
        }
        if (product.VendorId != vendorId.Value)
        {
            _logger.LogWarning("Unauthorized access attempt to restore product with ID: {ProductId}. User ID: {UserId}.", request.ProductId, userGuid);
            return Result<bool>.Failure("You do not have permission to restore this product.", ErrorType.Unauthorized);
        }

        product.Restore();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product with ID: {ProductId} has been successfully restored to Draft status.", request.ProductId);

        return Result<bool>.Success(true);
    }
}

public class RestoreProductCommandValidator : AbstractValidator<RestoreProductCommand>
{
    public RestoreProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product ID is required.");
    }
}