using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Cart.Commands;

public record ClearCartCommand() : IRequest<Result<bool>>;

public class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ClearCartCommandHandler> _logger;

    public ClearCartCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<ClearCartCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<bool>.Failure("Unauthorized: Invalid user token.", ErrorType.Unauthorized);

        Guid? customerId = await _unitOfWork.CustomerProfiles.GetCustomerIdByUserIdAsync(userGuid, cancellationToken);
        if (customerId == null)
            return Result<bool>.Failure("Customer profile not found.", ErrorType.NotFound);

        var cart = await _unitOfWork.Carts.GetActiveCartByCustomerIdAsync(customerId.Value, cancellationToken);
        if (cart == null)
            return Result<bool>.Failure("No active cart found.", ErrorType.NotFound);
        cart.ClearCart();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Cart {CartId} has been cleared for Customer {CustomerId}.", cart.Id, customerId.Value);

        return Result<bool>.Success(true);
    }
}