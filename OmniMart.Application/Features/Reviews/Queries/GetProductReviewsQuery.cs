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

namespace OmniMart.Application.Features.Reviews.Queries;

public record ReviewDto(
    Guid ReviewId,
    string CustomerName,
    int Rating,
    string? Comment,
    DateTime CreatedAt
);

public record ProductReviewsSummaryDto(
    Guid ProductId,
    double AverageRating,
    int TotalReviews,
    PaginatedResult<ReviewDto> Reviews
);

public record GetProductReviewsQuery(Guid ProductId, int Page = 1, int PageSize = 10) : IRequest<Result<ProductReviewsSummaryDto>>;

public class GetProductReviewsQueryHandler : IRequestHandler<GetProductReviewsQuery, Result<ProductReviewsSummaryDto>>
{
    private readonly IAppDbContext _context;
    private readonly ILogger<GetProductReviewsQueryHandler> _logger;

    public GetProductReviewsQueryHandler(IAppDbContext context, ILogger<GetProductReviewsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<ProductReviewsSummaryDto>> Handle(GetProductReviewsQuery request, CancellationToken cancellationToken)
    {
        bool productExists = await _context.Products.AnyAsync(p => p.Id == request.ProductId, cancellationToken);
        if (!productExists)
        {
            _logger.LogWarning("Attempted to fetch reviews for non-existent product: {ProductId}", request.ProductId);
            return Result<ProductReviewsSummaryDto>.Failure("Product not found.", ErrorType.NotFound);
        }

        var baseQuery = _context.ProductReviews
            .AsNoTracking()
            .Where(r => r.ProductId == request.ProductId);

        var stats = await baseQuery
            .GroupBy(r => r.ProductId)
            .Select(g => new
            {
                TotalCount = g.Count(),
                Average = g.Average(r => (double)r.Rating)
            })
            .FirstOrDefaultAsync(cancellationToken);

        int totalCount = stats?.TotalCount ?? 0;
        double averageRating = stats != null ? Math.Round(stats.Average, 1) : 0.0; 

        var reviewsList = await baseQuery
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ReviewDto(
                r.Id,
                $"{r.Customer.User!.FirstName} {r.Customer.User.LastName}", 
                r.Rating,
                r.Comment,
                r.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        var paginatedReviews = new PaginatedResult<ReviewDto>(reviewsList, totalCount, request.Page, request.PageSize);

        var summary = new ProductReviewsSummaryDto(request.ProductId, averageRating, totalCount, paginatedReviews);

        return Result<ProductReviewsSummaryDto>.Success(summary);
    }
}

public class GetProductReviewsQueryValidator : AbstractValidator<GetProductReviewsQuery>
{
    public GetProductReviewsQueryValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product ID is required.");
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("Page must be greater than 0.");
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(50).WithMessage("Page size must be between 1 and 50.");
    }
}