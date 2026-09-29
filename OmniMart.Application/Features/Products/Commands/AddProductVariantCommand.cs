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

public record AddProductVariantCommand(
    Guid ProductId,
    string SKU,
    decimal Price,
    int StockQuantity
) : IRequest<Result<bool>>;
public class AddProductVariantCommandHandler : IRequestHandler<AddProductVariantCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddProductVariantCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public AddProductVariantCommandHandler(IUnitOfWork unitOfWork, ILogger<AddProductVariantCommandHandler> logger, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(AddProductVariantCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling AddProductVariantCommand for ProductId: {ProductId}, SKU: {SKU}", request.ProductId, request.SKU);
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
        {
            return Result<bool>.Failure("Unauthorized: Invalid user token.", ErrorType.Unauthorized);
        }

        Guid? vendorId = await _unitOfWork.VendorProfiles.GetVendorIdByUserIdAsync(userGuid, cancellationToken);
        if (vendorId is null)
        {
            return Result<bool>.Failure("Vendor profile not found.", ErrorType.NotFound);
        }
        var product = await _unitOfWork.Products.GetProductWithAllVariantsForUpdateAsync(request.ProductId, cancellationToken); if (product is null)
        {
            return Result<bool>.Failure("Product not found.", ErrorType.NotFound);
        }
        if (product.VendorId != vendorId.Value)
        {
            return Result<bool>.Failure("You do not have permission to modify this product.", ErrorType.Unauthorized);
        }
        try
        {
            product.AddVariant(request.SKU, request.Price, request.StockQuantity);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Domain validation failed for Variant SKU {SKU}: {Message}", request.SKU, ex.Message);
            return Result<bool>.Failure(ex.Message, ErrorType.Validation);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully added/restored variant {SKU} for product {ProductId}.", request.SKU, request.ProductId);

        return Result<bool>.Success(true);
    }
}
    public class AddProductVariantCommandValidator : AbstractValidator<AddProductVariantCommand>
    {
        public AddProductVariantCommandValidator()
        {
            RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product ID is required.");
            RuleFor(x => x.SKU).NotEmpty().WithMessage("SKU is required.");
            RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");
            RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");
        }
    }