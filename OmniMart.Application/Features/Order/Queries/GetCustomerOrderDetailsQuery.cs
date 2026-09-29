using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Orders.Queries;

public record AddressDto(
    string City,
    string Street,
    string ZipCode,
    string PhoneNumber
);

public record OrderItemDetailDto(
    Guid ProductVariantId,
    string ProductName,
    string SKU,
    decimal UnitPrice,
    int Quantity
);

public record OrderDetailsDto(
    Guid OrderId,
    string Status,
    decimal TotalAmount,
    DateTimeOffset CreatedAt,
    string? TransactionId,
    AddressDto ShippingAddress,
    byte[] RowVersion,
    List<OrderItemDetailDto> Items
);

public record GetCustomerOrderDetailsQuery(Guid OrderId) : IRequest<Result<OrderDetailsDto>>;

public class GetCustomerOrderDetailsQueryHandler : IRequestHandler<GetCustomerOrderDetailsQuery, Result<OrderDetailsDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCustomerOrderDetailsQueryHandler(IAppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<OrderDetailsDto>> Handle(GetCustomerOrderDetailsQuery request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<OrderDetailsDto>.Failure("Unauthorized.", ErrorType.Unauthorized);

        var customerId = await _context.CustomerProfiles
            .AsNoTracking()
            .Where(c => c.UserId == userGuid)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (customerId == null)
            return Result<OrderDetailsDto>.Failure("Customer profile not found.", ErrorType.NotFound);

        var orderDto = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == request.OrderId && o.CustomerId == customerId.Value)
            .Select(o => new OrderDetailsDto(
                o.Id,
                o.Status.ToString(),
                o.TotalAmount,
                o.OrderDate,
                o.PaymentTransactionId,
                new AddressDto(
                    o.ShippingAddress.City,
                    o.ShippingAddress.Street,
                    o.ShippingAddress.ZipCode,
                    o.ShippingAddress.PhoneNumber
                ),
                o.RowVersion,
                o.OrderItems.Select(i => new OrderItemDetailDto(
                    i.ProductVariantId,
                    i.ProductVariant!.Product!.Name,
                    i.ProductVariant.SKU,
                    i.Price,
                    i.Quantity
               )).ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (orderDto == null)
            return Result<OrderDetailsDto>.Failure("Order not found.", ErrorType.NotFound);

        return Result<OrderDetailsDto>.Success(orderDto);
    }
    public class GetCustomerOrderDetailsQueryValidator : AbstractValidator<GetCustomerOrderDetailsQuery>
    {
        public GetCustomerOrderDetailsQueryValidator() => RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order ID is required.");
    }
}