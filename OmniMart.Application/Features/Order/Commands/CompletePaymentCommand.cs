using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Orders.Commands;

public record CompletePaymentCommand(Guid PaymentGroupId, string StripeSessionId) : IRequest<Result<bool>>;

public class CompletePaymentCommandHandler : IRequestHandler<CompletePaymentCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CompletePaymentCommandHandler> _logger;

    public CompletePaymentCommandHandler(IUnitOfWork unitOfWork, ILogger<CompletePaymentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(CompletePaymentCommand request, CancellationToken cancellationToken)
    {
        var paymentGroup = await _unitOfWork.PaymentGroups.GetAsync(
            p => p.Id == request.PaymentGroupId,
            cancellationToken,
            p => p.Orders
        );

        if (paymentGroup == null)
            return Result<bool>.Failure("PaymentGroup not found.", ErrorType.NotFound);

        if (paymentGroup.IsPaid)
            return Result<bool>.Success(true);

        paymentGroup.MarkAsPaid(request.StripeSessionId);

        var vendorIds = paymentGroup.Orders.Select(o => o.VendorId).Distinct().ToList();
        var vendors = await _unitOfWork.VendorProfiles.GetVendorsByIdsAsync(vendorIds, cancellationToken);
        foreach (var order in paymentGroup.Orders)
        {
            var vendor = vendors.FirstOrDefault(v => v.Id == order.VendorId);
            if (vendor != null)
            {
                vendor.ReceiveSaleRevenue(order.TotalAmount);
            }
         else
        {
            _logger.LogCritical($"URGENT: Vendor {order.VendorId} not found. Revenue not distributed for Order {order.Id}");
        }
    }

        var allOrderItems = paymentGroup.Orders.SelectMany(o => o.OrderItems).ToList();

        var variantIds = allOrderItems.Select(i => i.ProductVariantId).Distinct().ToList();

        var variants = await _unitOfWork.Products.GetVariantsByIdsAsync(variantIds, cancellationToken);

        foreach (var item in allOrderItems)
        {
            var variant = variants.FirstOrDefault(v => v.Id == item.ProductVariantId);
            if (variant != null)
            {
                variant.CommitReservedStock(item.Quantity);
            }
            else
            {
                _logger.LogCritical(
                    "URGENT: Product Variant {VariantId} not found. Stock deduction skipped for PaymentGroup {PaymentGroupId}! Manual refund might be required.",
                    item.ProductVariantId, request.PaymentGroupId);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Success: PaymentGroup {PaymentGroupId} marked as Paid.", request.PaymentGroupId);

        return Result<bool>.Success(true);
    }
    public class CompletePaymentCommandValidator : AbstractValidator<CompletePaymentCommand>
    {
        public CompletePaymentCommandValidator()
        {
            RuleFor(x => x.PaymentGroupId).NotEmpty().WithMessage("Payment Group ID is required.");
            RuleFor(x => x.StripeSessionId).NotEmpty().WithMessage("Stripe Session ID is required.");
        }
    }
}