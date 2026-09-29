using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Enums;
using System;

namespace OmniMart.Application.Features.Products.Queries;

public record VendorProductSummaryDto(
    Guid Id,
    string Name,
    decimal BasePrice,
    ProductStatus Status,
    bool IsPublished,
    string? RejectionReason,
    DateTimeOffset CreatedAt
);

public record GetVendorProductsQuery(
    ProductStatus? Status,
    bool? IsDeleted = false, 
    int Page = 1,
    int PageSize = 10
) : IRequest<Result<PaginatedResult<VendorProductSummaryDto>>>;
public class GetVendorProductsQueryHandler : IRequestHandler<GetVendorProductsQuery, Result<PaginatedResult<VendorProductSummaryDto>>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetVendorProductsQueryHandler> _logger;
    public GetVendorProductsQueryHandler(IAppDbContext context, ICurrentUserService currentUserService, ILogger<GetVendorProductsQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }
    public async Task<Result<PaginatedResult<VendorProductSummaryDto>>> Handle(GetVendorProductsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching vendor products. Page: {Page}, PageSize: {PageSize}, Status: {Status}, IsDeleted: {IsDeleted}", request.Page, request.PageSize, request.Status, request.IsDeleted);
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
        {
            _logger.LogWarning("Unauthorized access attempt. Invalid user token.");
            return Result<PaginatedResult<VendorProductSummaryDto>>.Failure("Unauthorized: Invalid user token.", ErrorType.Unauthorized);
        }

        var vendorId = await _context.VendorProfiles
            .AsNoTracking() 
            .Where(v => v.UserId == userGuid)
            .Select(v => (Guid?)v.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (vendorId is null)
        {
            _logger.LogWarning("Vendor profile not found for user ID: {UserId}", userGuid);
            return Result<PaginatedResult<VendorProductSummaryDto>>.Failure("Vendor profile not found.", ErrorType.NotFound);
        }
        var baseQuery = _context.Products
    .AsNoTracking()
    .Where(p => p.VendorId == vendorId.Value);

        if (request.IsDeleted == true)
        {
            _logger.LogInformation("Filtering for deleted products.");
            baseQuery = baseQuery.Where(p => p.IsDeleted);
        }
        else
        {
            _logger.LogInformation("Filtering for non-deleted products.");
            baseQuery = baseQuery.Where(p => !p.IsDeleted);
        }
        if (request.Status.HasValue)
        {
            _logger.LogInformation("Filtering by status: {Status}", request.Status.Value);
            baseQuery = baseQuery.Where(p => p.Status == request.Status.Value);
        }

        int totalCount = await baseQuery.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            var emptyResult = new PaginatedResult<VendorProductSummaryDto>(new List<VendorProductSummaryDto>(), totalCount, request.Page, request.PageSize);
            _logger.LogInformation("No products found for the given criteria. Returning empty result.");
            return Result<PaginatedResult<VendorProductSummaryDto>>.Success(emptyResult);

        }

        var products = await baseQuery
            .OrderByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new VendorProductSummaryDto(
                p.Id,
                p.Name,
                p.BasePrice,
                p.Status,
                p.IsPublished,
                p.RejectionReason,
                p.CreatedAt
            ))
            .ToListAsync(cancellationToken);
        var paginatedResult = new PaginatedResult<VendorProductSummaryDto>(products, totalCount, request.Page, request.PageSize);

        return Result<PaginatedResult<VendorProductSummaryDto>>.Success(paginatedResult);
    }
}

public class GetVendorProductsQueryValidator : AbstractValidator<GetVendorProductsQuery>
{
    public GetVendorProductsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page number must be greater than 0.");
        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0.")
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100.");
    }
}