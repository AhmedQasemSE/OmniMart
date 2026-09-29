using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Products.Queries.GetProductById;

public record ProductVariantDto(
    string SKU,
    decimal Price,
    int StockQuantity
);

public record ProductDetailDto(
    Guid Id,
    string Name,
    byte[] RowVersion,
    string Description,
    decimal BasePrice,
    List<ProductVariantDto> Variants
);

public record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductDetailDto>>, ICacheableQuery
{
    public string CacheKey => $"Product_Details_{Id}";

    public TimeSpan Expiration => TimeSpan.FromMinutes(15);
}

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDetailDto>>
{
    private readonly IAppDbContext _context;
    private readonly ILogger<GetProductByIdQueryHandler> _logger;

    public GetProductByIdQueryHandler(IAppDbContext context, ILogger<GetProductByIdQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<ProductDetailDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var productDetailDto = await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == request.Id && !p.IsDeleted && p.VendorProfile!.IsApproved)
            .Select(p => new ProductDetailDto(
                p.Id,
                p.Name,
                p.RowVersion,
                p.Description,
                p.BasePrice,
                p.Variants.Select(v => new ProductVariantDto(
                    v.SKU,
                    v.Price,
                    v.StockQuantity
                )).ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (productDetailDto == null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found.", request.Id);
            return Result<ProductDetailDto>.Failure("Product not found.", ErrorType.NotFound);
        }

        _logger.LogInformation("Successfully retrieved product details for ProductId: {ProductId}", request.Id);
        return Result<ProductDetailDto>.Success(productDetailDto);
    }
}

public class GetProductByIdQueryValidator : AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty).WithMessage("Product ID must be a valid non-empty GUID.");
    }
}