using Microsoft.EntityFrameworkCore;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Domain.Entities;
using OmniMart.Infrastructure.Data;

namespace OmniMart.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<bool> IsEmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<bool> IsAccountNumberExistsAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(u => u.AccountNumber == accountNumber, cancellationToken);
    }

    public async Task<bool> IsPhoneNumberExistsAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(u => u.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }
}