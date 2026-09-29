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

namespace OmniMart.Application.Features.Attributes.Queries;

public record ProductAttributeDto(Guid Id, string Name);

public record GetAllProductAttributesQuery(int PageNumber = 1, int PageSize = 10) : IRequest<Result<PaginatedResult<ProductAttributeDto>>>, ICacheableQuery
{
    public string CacheKey => $"{CacheKeys.ProductAttributesPrefix}{PageNumber}_Size_{PageSize}";
    public TimeSpan Expiration => TimeSpan.FromDays(1);
}

public class GetAllProductAttributesQueryHandler : IRequestHandler<GetAllProductAttributesQuery, Result<PaginatedResult<ProductAttributeDto>>>
{
    private readonly IAppDbContext _context;
    private readonly ILogger<GetAllProductAttributesQueryHandler> _logger;

    public GetAllProductAttributesQueryHandler(IAppDbContext context, ILogger<GetAllProductAttributesQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<PaginatedResult<ProductAttributeDto>>> Handle(GetAllProductAttributesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ProductAttributes.AsNoTracking();

        int totalCount = await query.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            var emptyResult = new PaginatedResult<ProductAttributeDto>(new List<ProductAttributeDto>(), totalCount, request.PageNumber, request.PageSize);
            return Result<PaginatedResult<ProductAttributeDto>>.Success(emptyResult);
        }

        var attributes = await query
            .OrderBy(a => a.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new ProductAttributeDto(a.Id, a.Name))
            .ToListAsync(cancellationToken);

        var paginatedResult = new PaginatedResult<ProductAttributeDto>(attributes, totalCount, request.PageNumber, request.PageSize);

        return Result<PaginatedResult<ProductAttributeDto>>.Success(paginatedResult);
    }
}

public class GetAllProductAttributesQueryValidator : AbstractValidator<GetAllProductAttributesQuery>
{
    public GetAllProductAttributesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}