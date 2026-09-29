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

public record ProcessOrderCommand(Guid OrderId, byte[] RowVersion) : IRequest<Result<bool>>;

public class ProcessOrderCommandHandler : IRequestHandler<ProcessOrderCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessOrderCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppDbContext _Context;
    public ProcessOrderCommandHandler(IUnitOfWork unitOfWork, ILogger<ProcessOrderCommandHandler> logger, IAppDbContext context, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _Context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(ProcessOrderCommand request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<bool>.Failure("Unauthorized.", ErrorType.Unauthorized);

        Guid? vendorId = await _unitOfWork.VendorProfiles.GetVendorIdByUserIdAsync(userGuid, cancellationToken);
        if (vendorId == null)
            return Result<bool>.Failure("Vendor profile not found.", ErrorType.NotFound);

        var order = await _unitOfWork.Orders.GetOrderWithDetailsAsync(request.OrderId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found.", request.OrderId);
            return Result<bool>.Failure("Order not found.", ErrorType.NotFound);
        }

        if (order.VendorId != vendorId.Value)
        {
            _logger.LogWarning("Vendor {VendorId} attempted to process Order {OrderId} which belongs to another vendor.", vendorId.Value, order.Id);
            return Result<bool>.Failure("You do not have permission to modify this order.", ErrorType.Unauthorized);
        }
        try
        {
            order.ChangeOrderStatus(OrderStatus.Processing);

            _unitOfWork.Orders.SetOriginalRowVersion(order, request.RowVersion);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} status changed to Processing by Vendor {VendorId}.", order.Id, vendorId.Value);
            return Result<bool>.Success(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict for Order {OrderId}.", request.OrderId);
            return Result<bool>.Failure("Sorry, the status of this request has just been modified by another user. Please refresh the page.", ErrorType.Conflict);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to change status for Order {OrderId}.", order.Id);
            return Result<bool>.Failure(ex.Message, ErrorType.Validation);
        }
    }
    
}
public class ProcessOrderCommandValidator : AbstractValidator<ProcessOrderCommand>
{
    public ProcessOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order ID is required.");
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("RowVersion is required.");
    }
}