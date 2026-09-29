using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace OmniMart.Application.Features.Cart.Commands;

public record AddToCartCommand(Guid ProductVariantId, int Quantity) : IRequest<Result<Guid>>;

public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, Result<Guid>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddToCartCommandHandler> _logger;
    public AddToCartCommandHandler(ICurrentUserService currentUserService, IUnitOfWork unitOfWork, ILogger<AddToCartCommandHandler> logger)
    {
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    public async Task<Result<Guid>> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
        {
            _logger.LogWarning("Unauthorized access attempt to add item to cart. Invalid user token.");    
            return Result<Guid>.Failure("Unauthorized: Invalid user token.", ErrorType.Unauthorized);
        }

        Guid? customerId = await _unitOfWork.CustomerProfiles.GetCustomerIdByUserIdAsync(userGuid, cancellationToken);
        if (customerId == null)
        {
            _logger.LogWarning("Customer profile not found. Only registered customers can add items to cart.");
            return Result<Guid>.Failure("Customer profile not found. Only registered customers can add items to cart.", ErrorType.NotFound);
        }
        var variant = await _unitOfWork.Products.GetVariantByIdAsync(request.ProductVariantId, cancellationToken);
        if (variant == null)
        {
            _logger.LogWarning("Product variant not found or has been deleted.");
            return Result<Guid>.Failure("Product variant not found or has been deleted.", ErrorType.NotFound);
        }
        if (!variant.Product!.IsPublished ||
            variant.Product.Status != ProductStatus.Active ||
            variant.Product.VendorProfile == null ||
            !variant.Product.VendorProfile.IsApproved)
        {
            _logger.LogWarning("Cannot add Product Variant {VariantId}. Product inactive or Vendor not approved.", request.ProductVariantId);
            return Result<Guid>.Failure("This product is not currently available for purchase.", ErrorType.Validation);
        }
        if (variant.AvailableStock < request.Quantity)
        {
            _logger.LogWarning("Not enough stock available for Product Variant {VariantId}. Requested: {RequestedQuantity}, Available: {AvailableStock}.", request.ProductVariantId, request.Quantity, variant.StockQuantity);
            return Result<Guid>.Failure($"Not enough stock available. Only {variant.AvailableStock} items left.", ErrorType.Conflict);
        }
        var cart = await _unitOfWork.Carts.GetActiveCartByCustomerIdAsync(customerId.Value, cancellationToken);

        if (cart == null)
        {
            _logger.LogInformation("No active cart found for Customer {CustomerId}. Creating a new cart.", customerId.Value);
            cart = new OmniMart.Domain.Entities.Cart(customerId.Value);
            await _unitOfWork.Carts.AddAsync(cart, cancellationToken);
        }
        var existingItem = cart.CartItems.FirstOrDefault(i => i.ProductVariantId == request.ProductVariantId);
        if (existingItem != null)
        {
            int totalRequestedQuantity = existingItem.Quantity + request.Quantity;
            if (totalRequestedQuantity > variant.AvailableStock)
            {
                _logger.LogWarning("Cannot add {RequestedQuantity} more of Product Variant {VariantId} to Cart {CartId}. Current quantity in cart: {CurrentQuantity}, Available stock: {AvailableStock}.", request.Quantity, request.ProductVariantId, cart.Id, existingItem.Quantity, variant.StockQuantity);
                return Result<Guid>.Failure($"Cannot add {request.Quantity} more. You already have {existingItem.Quantity} in cart, and only {variant.StockQuantity} are in stock.", ErrorType.Conflict);
            }
        }
        cart.AddItem(request.ProductVariantId, request.Quantity);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Item {VariantId} added to Cart {CartId} for Customer {CustomerId}.", request.ProductVariantId, cart.Id, customerId.Value);

        return Result<Guid>.Success(cart.Id);
    }
}

public class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.ProductVariantId)
            .NotEmpty().WithMessage("Product Variant ID is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be at least 1.")
            .LessThanOrEqualTo(50).WithMessage("You cannot add more than 50 items of the same product at once.");
    }
}