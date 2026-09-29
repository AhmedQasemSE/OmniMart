using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Cart.Commands;

public record RemoveCartItemCommand(Guid ProductVariantId) : IRequest<Result<bool>>;

public class RemoveCartItemCommandHandler : IRequestHandler<RemoveCartItemCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RemoveCartItemCommandHandler> _logger;

    public RemoveCartItemCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<RemoveCartItemCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
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

        cart.RemoveItem(request.ProductVariantId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Item {VariantId} removed from Cart {CartId}.", request.ProductVariantId, cart.Id);

        return Result<bool>.Success(true);
    }
}

public class RemoveCartItemCommandValidator : AbstractValidator<RemoveCartItemCommand>
{
    public RemoveCartItemCommandValidator()
    {
        RuleFor(x => x.ProductVariantId).NotEmpty().WithMessage("Product Variant ID is required.");
    }
}