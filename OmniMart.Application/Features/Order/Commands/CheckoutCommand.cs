using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Common;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;

namespace OmniMart.Application.Features.Orders.Commands;

public record CheckoutResponse(List<Guid> OrderIds, string PaymentUrl);

public record CheckoutCommand(Guid CustomerAddressId, string PaymentMethod, string? TransactionId = null) : IRequest<Result<CheckoutResponse>>;

public class CheckoutCommandHandler : IRequestHandler<CheckoutCommand, Result<CheckoutResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPaymentService _paymentService; 
    private readonly ILogger<CheckoutCommandHandler> _logger;

    public CheckoutCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IPaymentService paymentService,
        ILogger<CheckoutCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _paymentService = paymentService;
        _logger = logger;
    }
    public async Task<Result<CheckoutResponse>> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<CheckoutResponse>.Failure("Unauthorized.", ErrorType.Unauthorized);

        var customerProfile = await _unitOfWork.CustomerProfiles.GetAsync(c => c.UserId == userGuid, cancellationToken, c => c.Addresses);
        if (customerProfile == null) return Result<CheckoutResponse>.Failure("Customer profile not found.", ErrorType.NotFound);

        var selectedAddress = customerProfile.Addresses.FirstOrDefault(a => a.Id == request.CustomerAddressId);
        if (selectedAddress == null) return Result<CheckoutResponse>.Failure("Selected shipping address not found.", ErrorType.NotFound);

        var cart = await _unitOfWork.Carts.GetActiveCartByCustomerIdAsync(customerProfile.Id, cancellationToken);
        if (cart == null || !cart.CartItems.Any()) return Result<CheckoutResponse>.Failure("Your cart is empty.", ErrorType.Validation);

        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out PaymentMethod paymentMethodEnum))
            return Result<CheckoutResponse>.Failure("Invalid payment method.", ErrorType.Validation);

        var shippingAddress = new Address(selectedAddress.City, selectedAddress.Street, selectedAddress.ZipCode, selectedAddress.PhoneNumber);


        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var generatedOrderIds = new List<Guid>();

        var paymentGroup = new PaymentGroup(customerProfile.Id);

        try
        {
            var cartItemsByVendor = cart.CartItems.GroupBy(ci => ci.ProductVariant!.Product!.VendorId);

            foreach (var vendorGroup in cartItemsByVendor)
            {
                var order = new Order(customerProfile.Id, vendorGroup.Key, paymentGroup.Id, paymentMethodEnum, shippingAddress);

                if (!string.IsNullOrWhiteSpace(request.TransactionId))
                    order.SetTransactionId(request.TransactionId);

                foreach (var cartItem in vendorGroup)
                {
                    var variant = cartItem.ProductVariant!;
                    if (!variant.Product!.IsPublished || variant.Product.Status != ProductStatus.Active || variant.Product.VendorProfile == null || !variant.Product.VendorProfile.IsApproved)
                        throw new InvalidOperationException($"Product '{variant.Product!.Name}' is no longer available.");

                    if (variant.AvailableStock < cartItem.Quantity)
                        throw new InvalidOperationException($"Product '{variant.Product!.Name}' out of stock.");

                    order.AddOrderItem(variant.Id, variant.Price, cartItem.Quantity);

                    variant.ReserveStock(cartItem.Quantity);
                }

                paymentGroup.AddOrder(order);

                await _unitOfWork.Orders.AddAsync(order, cancellationToken);
                generatedOrderIds.Add(order.Id);
            }

            await _unitOfWork.PaymentGroups.AddAsync(paymentGroup, cancellationToken);

            cart.ClearCart();

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Checkout successful. Generated {Count} orders under PaymentGroup {PaymentGroupId}.", generatedOrderIds.Count, paymentGroup.Id);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Checkout error for Customer {CustomerId}.", customerProfile.Id);
            return Result<CheckoutResponse>.Failure("An error occurred during checkout.", ErrorType.Failure);
        }

        string paymentUrl = string.Empty;
        try
        {
            string realCustomerEmail = customerProfile?.User?.Email ?? "no-reply@omnimart.com";

            paymentUrl = await _paymentService.CreateCheckoutSessionAsync(
                paymentGroup.Id.ToString(), 
                paymentGroup.TotalAmount,   
                realCustomerEmail
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Stripe payment session.");
        }

        return Result<CheckoutResponse>.Success(new CheckoutResponse(generatedOrderIds, paymentUrl));
    }


}

public class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutCommandValidator()
    {
        RuleFor(x => x.CustomerAddressId).NotEmpty().WithMessage("Shipping address is required.");
        RuleFor(x => x.PaymentMethod).NotEmpty().WithMessage("Payment method is required.");
    }
}