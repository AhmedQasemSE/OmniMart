using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Cart.Commands;

public record UpdateCartItemQuantityCommand(Guid ProductVariantId, int NewQuantity) : IRequest<Result<bool>>;

public class UpdateCartItemQuantityCommandHandler : IRequestHandler<UpdateCartItemQuantityCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateCartItemQuantityCommandHandler> _logger;

    public UpdateCartItemQuantityCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<UpdateCartItemQuantityCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(UpdateCartItemQuantityCommand request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<bool>.Failure("Unauthorized.", ErrorType.Unauthorized);

        Guid? customerId = await _unitOfWork.CustomerProfiles.GetCustomerIdByUserIdAsync(userGuid, cancellationToken);
        if (customerId == null)
            return Result<bool>.Failure("Customer profile not found.", ErrorType.NotFound);

        var cart = await _unitOfWork.Carts.GetActiveCartByCustomerIdAsync(customerId.Value, cancellationToken);
        if (cart == null)
            return Result<bool>.Failure("No active cart found.", ErrorType.NotFound);

        var existingItem = cart.CartItems.FirstOrDefault(i => i.ProductVariantId == request.ProductVariantId);
        if (existingItem == null)
            return Result<bool>.Failure("This item is not in your cart.", ErrorType.NotFound);

        var variant = await _unitOfWork.Products.GetVariantByIdAsync(request.ProductVariantId, cancellationToken);
        if (variant == null)
            return Result<bool>.Failure("Product variant not found.", ErrorType.NotFound);

        if (variant.AvailableStock < request.NewQuantity)
        {
            _logger.LogWarning("Customer {CustomerId} attempted to update CartItem {VariantId} to {NewQuantity}, but only {Stock} available.",
                customerId.Value, request.ProductVariantId, request.NewQuantity, variant.AvailableStock);
            return Result<bool>.Failure($"Not enough stock available. Only {variant.AvailableStock} items left.", ErrorType.Conflict);
        }

        try
        {
            cart.UpdateItemQuantity(request.ProductVariantId, request.NewQuantity);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Customer {CustomerId} updated CartItem {VariantId} quantity to {NewQuantity}.",
                customerId.Value, request.ProductVariantId, request.NewQuantity);

            return Result<bool>.Success(true);
        }
        catch (InvalidOperationException ex)
        {
            return Result<bool>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public class UpdateCartItemQuantityCommandValidator : AbstractValidator<UpdateCartItemQuantityCommand>
{
    public UpdateCartItemQuantityCommandValidator()
    {
        RuleFor(x => x.ProductVariantId).NotEmpty().WithMessage("Product Variant ID is required.");

        RuleFor(x => x.NewQuantity)
            .GreaterThan(0).WithMessage("Quantity must be at least 1. If you want to remove the item, use the remove button.")
            .LessThanOrEqualTo(50).WithMessage("You cannot order more than 50 items of the same product at once.");
    }
}