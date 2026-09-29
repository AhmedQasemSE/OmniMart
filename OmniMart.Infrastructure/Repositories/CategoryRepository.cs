using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Domain.Entities;
using OmniMart.Infrastructure.Data;


namespace OmniMart.Infrastructure.Repositories;

public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
{
    public CategoryRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<bool> IsCategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(c => c.Id == categoryId, cancellationToken);
    }
    public async Task<bool> IsCategoryNameExistsUnderParentAsync(string name, Guid? parentId, CancellationToken cancellationToken = default) 
    {
        return await _dbSet.AnyAsync(n=>n.Name == name && n.ParentCategoryId == parentId, cancellationToken);
    }
    public async Task<bool> IsValidParentAsync(Guid categoryId, Guid? newParentId, CancellationToken cancellationToken = default)
    {
        if (newParentId == null)
            return true;

        if (categoryId == newParentId)
            return false;

        var allCategories = await _dbSet
            .Select(c => new { c.Id, c.ParentCategoryId })
            .ToDictionaryAsync(c => c.Id, c => c.ParentCategoryId, cancellationToken);

        if (!allCategories.ContainsKey(newParentId.Value))
            return false;

        if (allCategories[newParentId.Value] == categoryId)
            return false;

        int newParentDepth = 1;
        var currentParentId = allCategories[newParentId.Value];

        while (currentParentId != null)
        {
            newParentDepth++;

            if (newParentDepth >= 3)
                return false;

            if (allCategories.TryGetValue(currentParentId.Value, out var ancestorParentId))
            {
                if (currentParentId.Value == categoryId)
                    return false;

                currentParentId = ancestorParentId;
            }
            else
            {
                break;
            }
        }

        return true;
    }
}

