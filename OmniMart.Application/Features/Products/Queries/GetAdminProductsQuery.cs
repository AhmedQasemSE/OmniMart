using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Enums;

namespace OmniMart.Application.Features.Products.Queries.GetAdminProducts;

public record GetAdminProductsQuery(
    string? SearchTerm = null,
    Guid? VendorId = null,          
    ProductStatus? Status = null,   
    bool? IsPublished = null,       
    int Page = 1,
    int PageSize = 10
) : IRequest<Result<PaginatedResult<AdminProductDto>>>;

public record AdminProductDto(
    Guid Id,
    string Name,
    decimal BasePrice,
    ProductStatus Status,
    bool IsPublished,
    Guid VendorId,
    string VendorStoreName, 
    DateTimeOffset CreatedAt
);

public class GetAdminProductsQueryHandler : IRequestHandler<GetAdminProductsQuery, Result<PaginatedResult<AdminProductDto>>>
{
    private readonly IAppDbContext _context;

    public GetAdminProductsQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedResult<AdminProductDto>>> Handle(GetAdminProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Products.AsNoTracking()
            .Include(p => p.VendorProfile)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Variants.Any(v => v.SKU.ToLower().Contains(term))); 
        }
        if (request.VendorId.HasValue)
        {
            query = query.Where(p => p.VendorId == request.VendorId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }

        if (request.IsPublished.HasValue)
        {
            query = query.Where(p => p.IsPublished == request.IsPublished.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return Result<PaginatedResult<AdminProductDto>>.Success(
                new PaginatedResult<AdminProductDto>(new List<AdminProductDto>(), 0, request.Page, request.PageSize));
        }

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new AdminProductDto(
                p.Id,
                p.Name,
                p.BasePrice,
                p.Status,
                p.IsPublished,
                p.VendorId,
                p.VendorProfile != null ? p.VendorProfile.StoreName : "N/A",
                p.CreatedAt
            )).ToListAsync(cancellationToken);

        return Result<PaginatedResult<AdminProductDto>>.Success(
            new PaginatedResult<AdminProductDto>(products, totalCount, request.Page, request.PageSize));
    }
}

public class GetAdminProductsQueryValidator : AbstractValidator<GetAdminProductsQuery>
{
    public GetAdminProductsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}