using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;

namespace OmniMart.Application.Features.Products.Commands;

public record RemoveProductVariantCommand(Guid ProductId, string SKU) : IRequest<Result<bool>>;

public class RemoveProductVariantCommandHandler : IRequestHandler<RemoveProductVariantCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveProductVariantCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public RemoveProductVariantCommandHandler(IUnitOfWork unitOfWork, ILogger<RemoveProductVariantCommandHandler> logger, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(RemoveProductVariantCommand request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<bool>.Failure("Unauthorized: Invalid user token.", ErrorType.Unauthorized);

        Guid? vendorId = await _unitOfWork.VendorProfiles.GetVendorIdByUserIdAsync(userGuid, cancellationToken);
        if (vendorId is null) return Result<bool>.Failure("Vendor profile not found.", ErrorType.NotFound);

        var product = await _unitOfWork.Products.GetProductWithVariantsAsync(request.ProductId, cancellationToken);
        if (product is null) return Result<bool>.Failure("Product not found.", ErrorType.NotFound);

        if (product.VendorId != vendorId.Value)
            return Result<bool>.Failure("You do not have permission to modify this product.", ErrorType.Unauthorized);

        try
        {
            product.RemoveVariant(request.SKU);
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
        {
            _logger.LogWarning(ex, "Domain validation failed for removing Variant SKU {SKU}: {Message}", request.SKU, ex.Message);
            return Result<bool>.Failure(ex.Message, ErrorType.Validation);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully removed variant {SKU} for product {ProductId}.", request.SKU, request.ProductId);

        return Result<bool>.Success(true);
    }
}

public class RemoveProductVariantCommandValidator : AbstractValidator<RemoveProductVariantCommand>
{
    public RemoveProductVariantCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product ID is required.");
        RuleFor(x => x.SKU).NotEmpty().WithMessage("SKU is required.");
    }
}