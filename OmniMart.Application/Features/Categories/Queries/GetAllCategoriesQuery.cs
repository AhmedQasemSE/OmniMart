using MediatR;
using Microsoft.EntityFrameworkCore; 
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Categories.Queries.GetAllCategories;

public record CategoryDto(
    Guid Id,
    string Name,
    string Description,
    List<CategoryDto> SubCategories
);

public record GetAllCategoriesQuery : IRequest<Result<List<CategoryDto>>>, ICacheableQuery
{
    public string CacheKey => CacheKeys.AllCategories;

    public TimeSpan Expiration => TimeSpan.FromDays(1);
}

public class GetAllCategoriesQueryHandler : IRequestHandler<GetAllCategoriesQuery, Result<List<CategoryDto>>>
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<GetAllCategoriesQueryHandler> _logger;

    public GetAllCategoriesQueryHandler(IAppDbContext dbContext, ILogger<GetAllCategoriesQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<List<CategoryDto>>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
    {
        var allCategories = await _dbContext.Categories.AsNoTracking()
            .ToListAsync(cancellationToken);

        if (allCategories == null || !allCategories.Any())
        {
            _logger.LogInformation("No categories found in database.");
            return Result<List<CategoryDto>>.Success(new List<CategoryDto>());
        }

        var rootCategories = allCategories.Where(c => c.ParentCategoryId == null).ToList();

        CategoryDto BuildTree(Category currentCategory)
        {
            var children = allCategories
                .Where(c => c.ParentCategoryId == currentCategory.Id)
                .Select(BuildTree)
                .ToList();

            return new CategoryDto(
                currentCategory.Id,
                currentCategory.Name,
                currentCategory.Description,
                children
            );
        }

        var categoryTree = rootCategories.Select(BuildTree).ToList();

        _logger.LogInformation("Successfully retrieved and cached category tree.");
        return Result<List<CategoryDto>>.Success(categoryTree);
    }
}