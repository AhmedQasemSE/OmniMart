using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Enums;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Orders.Commands;

public record RefundOrderCommand(Guid OrderId, byte[] RowVersion) : IRequest<Result<bool>>;

public class RefundOrderCommandHandler : IRequestHandler<RefundOrderCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RefundOrderCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public RefundOrderCommandHandler(IUnitOfWork unitOfWork, ILogger<RefundOrderCommandHandler> logger, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(RefundOrderCommand request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<bool>.Failure("Unauthorized.", ErrorType.Unauthorized);

        var order = await _unitOfWork.Orders.GetOrderWithDetailsAsync(request.OrderId, cancellationToken);
        if (order == null) return Result<bool>.Failure("Order not found.", ErrorType.NotFound);

        if (_currentUserService.Role != SystemRole.Admin.ToString())
        {
            Guid? vendorId = await _unitOfWork.VendorProfiles.GetVendorIdByUserIdAsync(userGuid, cancellationToken);
            if (vendorId == null) return Result<bool>.Failure("Vendor profile not found.", ErrorType.NotFound);

            if (order.VendorId != vendorId.Value) 
                return Result<bool>.Failure("You do not have permission to modify this order.", ErrorType.Unauthorized);
        }

        try
        {
            var previousStatus = order.Status;

            order.ChangeOrderStatus(OrderStatus.Refunded);

            foreach (var item in order.OrderItems)
            {
                if (item.ProductVariant != null)
                {
                    item.ProductVariant.IncreaseStock(item.Quantity);
                }
            }

            bool wasPaid = previousStatus == OrderStatus.Processing ||
                           previousStatus == OrderStatus.Shipped ||
                           previousStatus == OrderStatus.Delivered;

            if (wasPaid)
            {
                var vendorProfile = await _unitOfWork.VendorProfiles.GetByIdAsync(order.VendorId, cancellationToken);
                if (vendorProfile != null)
                {
                    vendorProfile.DeductRefundAmount(order.TotalAmount);
                }

                var customerProfile = await _unitOfWork.CustomerProfiles.GetByIdAsync(order.CustomerId, cancellationToken);
                if (customerProfile != null)
                {
                    customerProfile.DeductPointsForRefund(order.TotalAmount);
                }
            }
            _unitOfWork.Orders.SetOriginalRowVersion(order, request.RowVersion);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} has been refunded. Stock restored. Balances adjusted if previously delivered.", order.Id);
            return Result<bool>.Success(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict for Order {OrderId}.", request.OrderId);
            return Result<bool>.Failure("Sorry, the status of this request has just been modified by another user. Please refresh the page.", ErrorType.Conflict);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation while refunding order {OrderId}.", request.OrderId);
            return Result<bool>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}
public class RefundOrderCommandValidator : AbstractValidator<RefundOrderCommand>
{
    public RefundOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order ID is required.");
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("RowVersion is required.");
    }
}