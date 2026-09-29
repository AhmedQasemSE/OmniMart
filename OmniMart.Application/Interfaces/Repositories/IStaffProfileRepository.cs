using OmniMart.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniMart.Application.Interfaces.Repositories;

public interface IStaffProfileRepository : IGenericRepository<StaffProfile>
{
    Task<Guid?> GetStaffIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
