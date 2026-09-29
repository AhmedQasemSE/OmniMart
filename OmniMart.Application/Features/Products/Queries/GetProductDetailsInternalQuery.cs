using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Enums;
using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace OmniMart.Application.Features.Products.Queries;

public record InternalProductVariantDto(
    string SKU,
    decimal Price,
    int StockQuantity,
    bool IsDeleted
);

public record ProductDetailsInternalDto(
    Guid Id,
    Guid VendorId,
    string StoreName,
    Guid CategoryId,
    string Name,
    string Description,
    decimal BasePrice,
    ProductStatus Status,
    bool IsPublished,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    string? RejectionReason,
    string? SuspensionReason,
    byte[] RowVersion,
    List<InternalProductVariantDto> Variants
);

public record GetProductDetailsInternalQuery(Guid ProductId) : IRequest<Result<ProductDetailsInternalDto>>;



public class GetProductDetailsInternalQueryHandler : IRequestHandler<GetProductDetailsInternalQuery, Result<ProductDetailsInternalDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetProductDetailsInternalQueryHandler> _logger;

    public GetProductDetailsInternalQueryHandler(IAppDbContext context, ICurrentUserService currentUserService, ILogger<GetProductDetailsInternalQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<ProductDetailsInternalDto>> Handle(GetProductDetailsInternalQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching product details for ProductId: {ProductId}", request.ProductId);
        string? userIdString = _currentUserService.UserId;
        string? userRole = _currentUserService.Role;

        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
        {
            _logger.LogWarning("Unauthorized access attempt with invalid user token.");
            return Result<ProductDetailsInternalDto>.Failure("Unauthorized: Invalid user token.", ErrorType.Unauthorized);
        }

        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.VendorProfile)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
        {
            _logger.LogWarning("Product not found for ProductId: {ProductId}", request.ProductId);
            return Result<ProductDetailsInternalDto>.Failure("Product not found.", ErrorType.NotFound);
        }

        if (userRole != SystemRole.Admin.ToString())
        {
            var vendorId = await _context.VendorProfiles
                .AsNoTracking()
                .Where(v => v.UserId == userGuid)
                .Select(v => (Guid?)v.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (vendorId is null || product.VendorId != vendorId.Value)
            {
                _logger.LogWarning("Unauthorized attempt by User {UserId} to view product {ProductId}.", userGuid, request.ProductId);
                return Result<ProductDetailsInternalDto>.Failure("You do not have permission to view this product details.", ErrorType.Unauthorized);
            }
        }
        var variantsDto = product.Variants.Select(v => new InternalProductVariantDto(
            v.SKU,
            v.Price,
            v.StockQuantity,
            v.IsDeleted
        )).ToList();

        var resultDto = new ProductDetailsInternalDto(
            product.Id,
            product.VendorId,
            product.VendorProfile!.StoreName, 
            product.CategoryId,
            product.Name,
            product.Description,
            product.BasePrice,
            product.Status,
            product.IsPublished,
            product.IsDeleted,
            product.CreatedAt,
            product.PublishedAt,
            product.RejectionReason,
            product.SuspensionReason,
            product.RowVersion,
            variantsDto 
        );
        _logger.LogInformation("Successfully fetched product details for ProductId: {ProductId}", request.ProductId);
        return Result<ProductDetailsInternalDto>.Success(resultDto);
    }
}

public class GetProductDetailsInternalQueryValidator : AbstractValidator<GetProductDetailsInternalQuery>
{
    public GetProductDetailsInternalQueryValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product ID is required.");
    }
}



