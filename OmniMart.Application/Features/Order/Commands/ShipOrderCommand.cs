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

public record ShipOrderCommand(Guid OrderId, byte[] RowVersion) : IRequest<Result<bool>>;

public class ShipOrderCommandHandler : IRequestHandler<ShipOrderCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ShipOrderCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppDbContext _Context;
    public ShipOrderCommandHandler(IUnitOfWork unitOfWork, ILogger<ShipOrderCommandHandler> logger, IAppDbContext context, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _Context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(ShipOrderCommand request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<bool>.Failure("Unauthorized.", ErrorType.Unauthorized);

        var order = await _unitOfWork.Orders.GetOrderWithDetailsAsync(request.OrderId, cancellationToken);
        if (order == null)
            return Result<bool>.Failure("Order not found.", ErrorType.NotFound);

        if (_currentUserService.Role != SystemRole.Admin.ToString())
        {
            Guid? vendorId = await _unitOfWork.VendorProfiles.GetVendorIdByUserIdAsync(userGuid, cancellationToken);
            if (vendorId == null) return Result<bool>.Failure("Vendor profile not found.", ErrorType.NotFound);

            if (order.VendorId != vendorId.Value)
            {
                return Result<bool>.Failure("You do not have permission to modify this order.", ErrorType.Unauthorized);
            }
        }

        try
        {
            order.ChangeOrderStatus(OrderStatus.Shipped);
            _unitOfWork.Orders.SetOriginalRowVersion(order, request.RowVersion);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Order {OrderId} status changed to Shipped.", order.Id);
            return Result<bool>.Success(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict for Order {OrderId}.", request.OrderId);
            return Result<bool>.Failure("Sorry, the status of this request has just been modified by another user. Please refresh the page.", ErrorType.Conflict);
        }
        catch (InvalidOperationException ex)
        {
            return Result<bool>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}
public class ShipOrderCommandValidator : AbstractValidator<ShipOrderCommand>
{
    public ShipOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("OrderId is required.");
    }
}