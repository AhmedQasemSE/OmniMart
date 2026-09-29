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

public record CancelOrderCommand(Guid OrderId, byte[] RowVersion) : IRequest<Result<bool>>;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelOrderCommandHandler> _logger;
    private readonly IAppDbContext _Context;
    private readonly ICurrentUserService _currentUserService; 

    public CancelOrderCommandHandler(IUnitOfWork unitOfWork, ILogger<CancelOrderCommandHandler> logger, IAppDbContext context, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _Context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<bool>.Failure("Unauthorized.", ErrorType.Unauthorized);

        var order = await _unitOfWork.Orders.GetOrderWithDetailsAsync(request.OrderId, cancellationToken);

        if (order == null)
            return Result<bool>.Failure("Order not found.", ErrorType.NotFound);

        string role = _currentUserService.Role ?? "";

        if (role != SystemRole.Admin.ToString())
        {
            if (role == SystemRole.Customer.ToString())
            {
                var customerId = await _unitOfWork.CustomerProfiles.GetCustomerIdByUserIdAsync(userGuid, cancellationToken);
                if (order.CustomerId != customerId)
                    return Result<bool>.Failure("You can only cancel your own orders.", ErrorType.Unauthorized);
            }
            else if (role == SystemRole.Vendor.ToString())
            {
                var vendorId = await _unitOfWork.VendorProfiles.GetVendorIdByUserIdAsync(userGuid, cancellationToken);
                if (order.VendorId != vendorId)
                    return Result<bool>.Failure("You do not have permission to cancel this order.", ErrorType.Unauthorized);
            }
        }
        try
        {
            bool wasPaid = order.Status == OrderStatus.Processing || order.Status == OrderStatus.Shipped;

            order.ChangeOrderStatus(OrderStatus.Cancelled);

            foreach (var item in order.OrderItems)
            {
                if (item.ProductVariant != null)
                {
                    if (wasPaid)
                    {
                        item.ProductVariant.IncreaseStock(item.Quantity);
                    }
                    else
                    {
                        item.ProductVariant.ReleaseReservedStock(item.Quantity);
                    }
                }
            }
            if (wasPaid)
            {
                var vendor = await _unitOfWork.VendorProfiles.GetByIdAsync(order.VendorId, cancellationToken);

                if (vendor != null)
                {
                    vendor.DeductRefundAmount(order.TotalAmount);
                }
            }

            _unitOfWork.Orders.SetOriginalRowVersion(order, request.RowVersion);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} has been cancelled. Stock restored. Vendor balance deducted (if it was paid).", order.Id);
            return Result<bool>.Success(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict for Order {OrderId}.", request.OrderId);
            return Result<bool>.Failure("Sorry, the status of this request has just been modified by another user. Please refresh the page.", ErrorType.Conflict);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to cancel Order {OrderId}.", order.Id);
            return Result<bool>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}
public class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order ID is required.");
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("RowVersion is required.");
    }
}