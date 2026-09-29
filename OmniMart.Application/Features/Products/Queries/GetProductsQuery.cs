using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Enums;

namespace OmniMart.Application.Features.Products.Queries.GetProducts;

public record GetProductsQuery(
    string? SearchTerm = null,
    Guid? CategoryId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int Page = 1,
    int PageSize = 10)
    : IRequest<Result<PaginatedResult<ProductSummaryDto>>>, ICacheableQuery
{
    public string CacheKey => $"Products_Search_{SearchTerm}_Cat_{CategoryId}_Min_{MinPrice}_Max_{MaxPrice}_Page_{Page}_Size_{PageSize}";
    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}
public record ProductSummaryDto(
    Guid Id,
    string Name,
    decimal BasePrice,
    ProductStatus Status,
    int VariantsCount
);
public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, Result<PaginatedResult<ProductSummaryDto>>>
{
    private readonly IAppDbContext _context;
    private readonly ILogger<GetProductsQueryHandler> _logger;

    public GetProductsQueryHandler(IAppDbContext context, ILogger<GetProductsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<PaginatedResult<ProductSummaryDto>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetProductsQuery with Search: {SearchTerm}, Page: {Page}, PageSize: {PageSize}", request.SearchTerm, request.Page, request.PageSize);

        var baseQuery = _context.Products.AsNoTracking()
                .Where(p => p.IsDeleted == false
                         &&p.IsPublished == true
                         && p.Status == ProductStatus.Active
                         && p.VendorProfile != null
                         && p.VendorProfile.IsApproved == true) 
                .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            baseQuery = baseQuery.Where(p =>
                p.Name.ToLower().Contains(term) ||
                (p.Category != null && p.Category.Name.ToLower().Contains(term)));
        }

        if (request.CategoryId.HasValue)
        {
            baseQuery = baseQuery.Where(p => p.CategoryId == request.CategoryId.Value);
        }

        if (request.MinPrice.HasValue)
        {
            baseQuery = baseQuery.Where(p => p.Variants.Any(v => v.Price >= request.MinPrice.Value));
        }

        if (request.MaxPrice.HasValue)
        {
            baseQuery = baseQuery.Where(p => p.Variants.Any(v => v.Price <= request.MaxPrice.Value));
        }

        int totalCount = await baseQuery.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            _logger.LogInformation("No products found for the given criteria. Returning empty result.");
            var emptyResult = new PaginatedResult<ProductSummaryDto>(new List<ProductSummaryDto>(), 0, request.Page, request.PageSize);
            return Result<PaginatedResult<ProductSummaryDto>>.Success(emptyResult);
        }

        var products = await baseQuery
            .OrderByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductSummaryDto(
                p.Id,
                p.Name,
                p.BasePrice,
                p.Status,
                p.Variants.Count
            )).ToListAsync(cancellationToken);

        var paginatedResult = new PaginatedResult<ProductSummaryDto>(products, totalCount, request.Page, request.PageSize);

        _logger.LogInformation("Products retrieved successfully.");
        return Result<PaginatedResult<ProductSummaryDto>>.Success(paginatedResult);
    }
}
public class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
        RuleFor(x => x)
            .Must(x => x.MinPrice <= x.MaxPrice)
            .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue)
            .WithMessage("The minimum price cannot be greater than the maximum price.")
            .WithErrorCode("ValidationError");
        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue);
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue);
    }
}