using OmniMart.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniMart.Application.Interfaces.Repositories;

public interface ICategoryRepository : IGenericRepository<Category>
{
    Task<bool> IsCategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<bool> IsCategoryNameExistsUnderParentAsync(string name, Guid? parentId, CancellationToken cancellationToken = default);
    Task<bool> IsValidParentAsync(Guid categoryId, Guid? newParentId, CancellationToken cancellationToken = default);
}
