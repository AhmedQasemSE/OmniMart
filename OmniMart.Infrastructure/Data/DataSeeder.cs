using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;

namespace OmniMart.Infrastructure.Data;

public class DataSeeder
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IConfiguration _configuration;

    public DataSeeder(AppDbContext context, IPasswordHasherService passwordHasher, IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task SeedAdminAsync()
    {
        bool adminExists = await _context.Users.AnyAsync(u => u.Role == SystemRole.SuperAdmin);

        if (adminExists)
        {
            return;
        }

        var adminPassword = _configuration["AdminSettings:DefaultPassword"];
        if (string.IsNullOrEmpty(adminPassword))
        {
            adminPassword = "FallbackStrongPassword@2026!";
        }

        var adminUser = new User(
            firstName: "System",
            lastName: "Administrator",
            email: "admin@omnimart.com",
            phoneNumber: "+900000000000",
            passwordHash: _passwordHasher.HashPassword(adminPassword),
            accountNumber: "ACC_ADMIN001",
            role: SystemRole.SuperAdmin
        );

        var adminProfile = new StaffProfile(
            userId: adminUser.Id,
            staffNumber: "STF_ADMIN001",
            departmentRole: Department.IT,
            salary: 0
        );

        await _context.Users.AddAsync(adminUser);
        await _context.StaffProfiles.AddAsync(adminProfile);

        await _context.SaveChangesAsync();
    }
}