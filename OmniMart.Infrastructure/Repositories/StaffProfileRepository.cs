using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Domain.Entities;
using OmniMart.Infrastructure.Data;


namespace OmniMart.Infrastructure.Repositories;

public class StaffProfileRepository: GenericRepository<StaffProfile>, IStaffProfileRepository
{
    public StaffProfileRepository(AppDbContext context) : base(context)
    {

    }
    public async Task<Guid?> GetStaffIdByUserIdAsync(Guid userId, CancellationToken cancellationToken=default)
    {
        return await _dbSet.Where(u=>u.UserId==userId).Select(u => (Guid?) u.Id).FirstOrDefaultAsync(cancellationToken);
    }
}
