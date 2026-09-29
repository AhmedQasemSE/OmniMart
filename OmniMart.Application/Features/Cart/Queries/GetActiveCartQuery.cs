using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;



namespace OmniMart.Application.Features.Cart.Queries;

public record CartItemDto(
    Guid ProductVariantId,
    string ProductName,
    string SKU,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice
);

public record ActiveCartDto(
    Guid CartId,
    List<CartItemDto> Items,
    decimal GrandTotal
);

public record GetActiveCartQuery() : IRequest<Result<ActiveCartDto>>;

public class GetActiveCartQueryHandler : IRequestHandler<GetActiveCartQuery, Result<ActiveCartDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetActiveCartQueryHandler> _logger;

    public GetActiveCartQueryHandler(IAppDbContext context, ICurrentUserService currentUserService, ILogger<GetActiveCartQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<ActiveCartDto>> Handle(GetActiveCartQuery request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<ActiveCartDto>.Failure("Unauthorized: Invalid user token.", ErrorType.Unauthorized);

        var customerId = await _context.CustomerProfiles
            .AsNoTracking()
            .Where(c => c.UserId == userGuid)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (customerId == null)
            return Result<ActiveCartDto>.Failure("Customer profile not found.", ErrorType.NotFound);

        var cartDto = await _context.Carts
            .AsNoTracking()
            .Where(c => c.CustomerId == customerId.Value)
            .Select(c => new ActiveCartDto(
                c.Id,
                c.CartItems.Where(ci => !ci.ProductVariant!.IsDeleted && !ci.ProductVariant.Product!.IsDeleted)
               .Select(ci => new CartItemDto(
                    ci.ProductVariantId,
                    ci.ProductVariant!.Product!.Name,
                    ci.ProductVariant.SKU,
                    ci.ProductVariant.Price,
                    ci.Quantity,
                    ci.Quantity * ci.ProductVariant.Price
                )).ToList(),
                c.CartItems.Where(ci => !ci.ProductVariant!.IsDeleted && !ci.ProductVariant.Product!.IsDeleted)
               .Sum(ci => ci.Quantity * ci.ProductVariant!.Price)
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (cartDto == null)
        {
            var emptyCart = new ActiveCartDto(Guid.Empty, new List<CartItemDto>(), 0);
            return Result<ActiveCartDto>.Success(emptyCart);
        }

        _logger.LogInformation("Active cart retrieved for Customer {CustomerId}.", customerId.Value);
        return Result<ActiveCartDto>.Success(cartDto);
    }

}