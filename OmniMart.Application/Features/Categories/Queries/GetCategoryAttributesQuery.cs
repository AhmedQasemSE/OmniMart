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

namespace OmniMart.Application.Features.Categories.Queries;

public record CategoryAttributeResponseDto(Guid AttributeId, string Name, bool IsRequired);

public record GetCategoryAttributesQuery(Guid CategoryId) : IRequest<Result<List<CategoryAttributeResponseDto>>>, ICacheableQuery
{
    public string CacheKey => $"Category_Attributes_{CategoryId}";
    public TimeSpan Expiration => TimeSpan.FromHours(12); 
}

public class GetCategoryAttributesQueryHandler : IRequestHandler<GetCategoryAttributesQuery, Result<List<CategoryAttributeResponseDto>>>
{
    private readonly IAppDbContext _context;
    private readonly ILogger<GetCategoryAttributesQueryHandler> _logger;

    public GetCategoryAttributesQueryHandler(IAppDbContext context, ILogger<GetCategoryAttributesQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<List<CategoryAttributeResponseDto>>> Handle(GetCategoryAttributesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching attributes for Category ID: {CategoryId}", request.CategoryId);

        var attributes = await _context.CategoryAttributes
            .AsNoTracking()
            .Where(ca => ca.CategoryId == request.CategoryId)
            .Select(ca => new CategoryAttributeResponseDto(
                ca.ProductAttributeId,
                ca.ProductAttribute!.Name, 
                ca.IsRequired
            ))
            .ToListAsync(cancellationToken);

        return Result<List<CategoryAttributeResponseDto>>.Success(attributes);
    }
}

public class GetCategoryAttributesQueryValidator : AbstractValidator<GetCategoryAttributesQuery>
{
    public GetCategoryAttributesQueryValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Category ID is required.");
    }
}