using FluentValidation;
using MediatR;
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

public record DeliverOrderCommand(Guid OrderId, byte[] RowVersion) : IRequest<Result<bool>>;

public class DeliverOrderCommandHandler : IRequestHandler<DeliverOrderCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeliverOrderCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public DeliverOrderCommandHandler(IUnitOfWork unitOfWork, ILogger<DeliverOrderCommandHandler> logger, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(DeliverOrderCommand request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<bool>.Failure("Unauthorized.", ErrorType.Unauthorized);

        var order = await _unitOfWork.Orders.GetOrderWithDetailsAsync(request.OrderId, cancellationToken);
        if (order == null) return Result<bool>.Failure("Order not found.", ErrorType.NotFound);

        if (_currentUserService.Role != SystemRole.Admin.ToString())
        {
            Guid? currentVendorId = await _unitOfWork.VendorProfiles.GetVendorIdByUserIdAsync(userGuid, cancellationToken);
            if (currentVendorId == null) return Result<bool>.Failure("Vendor profile not found.", ErrorType.NotFound);

            if (order.VendorId != currentVendorId.Value)
            {
                return Result<bool>.Failure("You do not have permission to modify this order.", ErrorType.Unauthorized);
            }
        }

        try
        {
            order.ChangeOrderStatus(OrderStatus.Delivered);

            var customerProfile = await _unitOfWork.CustomerProfiles.GetByIdAsync(order.CustomerId, cancellationToken);
            if (customerProfile != null) customerProfile.RecordPurchase(order.TotalAmount);


            _unitOfWork.Orders.SetOriginalRowVersion(order, request.RowVersion);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Order {OrderId} delivered. Points added and revenues distributed.", order.Id);

            return Result<bool>.Success(true);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict for Order {OrderId}.", request.OrderId);
            return Result<bool>.Failure("Sorry, the status of this request has just been modified by another user. Please refresh the page.", ErrorType.Conflict);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation while delivering order {OrderId}.", request.OrderId);
            return Result<bool>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public class DeliverOrderCommandValidator : AbstractValidator<DeliverOrderCommand>
{
    public DeliverOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order ID is required.");
    }
}