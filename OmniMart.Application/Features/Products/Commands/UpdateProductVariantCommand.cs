using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;

namespace OmniMart.Application.Features.Products.Commands;

public record UpdateProductVariantCommand(
    Guid ProductId,
    string SKU,
    decimal Price,
    int StockQuantity
) : IRequest<Result<bool>>;

public class UpdateProductVariantCommandHandler : IRequestHandler<UpdateProductVariantCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProductVariantCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public UpdateProductVariantCommandHandler(IUnitOfWork unitOfWork, ILogger<UpdateProductVariantCommandHandler> logger, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(UpdateProductVariantCommand request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<bool>.Failure("Unauthorized: Invalid user token.", ErrorType.Unauthorized);

        Guid? vendorId = await _unitOfWork.VendorProfiles.GetVendorIdByUserIdAsync(userGuid, cancellationToken);
        if (vendorId is null)
            return Result<bool>.Failure("Vendor profile not found.", ErrorType.NotFound);

        var product = await _unitOfWork.Products.GetProductWithAllVariantsForUpdateAsync(request.ProductId, cancellationToken);

        if (product is null)
            return Result<bool>.Failure("Product not found.", ErrorType.NotFound);

        if (product.VendorId != vendorId.Value)
            return Result<bool>.Failure("You do not have permission to modify this product.", ErrorType.Unauthorized);

        try
        {
            product.UpdateVariant(request.SKU, request.Price, request.StockQuantity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated variant {SKU} for product {ProductId}.", request.SKU, request.ProductId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
        {
            _logger.LogWarning(ex, "Domain validation failed for Variant SKU {SKU}: {Message}", request.SKU, ex.Message);
            return Result<bool>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public class UpdateProductVariantCommandValidator : AbstractValidator<UpdateProductVariantCommand>
{
    public UpdateProductVariantCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product ID is required.");
        RuleFor(x => x.SKU).NotEmpty().WithMessage("SKU is required.");
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");
    }
}