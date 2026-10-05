using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Enums;
using OmniMart.Infrastructure.Data;
using OmniMart.Tests.Builders;
using System;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Infrastructure;

public class DataSeederTests : IDisposable
{
    private readonly Mock<IPasswordHasherService> _passwordHasherMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly AppDbContext _dbContext;
    private readonly DataSeeder _seeder;

    public DataSeederTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _passwordHasherMock = new Mock<IPasswordHasherService>();
        _configurationMock = new Mock<IConfiguration>();

        _seeder = new DataSeeder(_dbContext, _passwordHasherMock.Object, _configurationMock.Object);
    }

    private void SetupDefaultSuccessBehavior()
    {
        _passwordHasherMock
            .Setup(x => x.HashPassword(It.IsAny<string>()))
            .Returns("HashedPassword123");

        _configurationMock
            .Setup(x => x["AdminSettings:DefaultPassword"])
            .Returns("ConfigPassword123!");
    }

    [Fact]
    public async Task SeedAdminAsync_ShouldCreateAdminAndProfile_WhenNoAdminExists()
    {
        SetupDefaultSuccessBehavior();

        await _seeder.SeedAdminAsync();

        var adminUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Role == SystemRole.SuperAdmin);
        var staffProfile = await _dbContext.StaffProfiles.FirstOrDefaultAsync(s => s.UserId == adminUser!.Id);

        adminUser.Should().NotBeNull();
        adminUser!.Email.Should().Be("admin@omnimart.com");

        staffProfile.Should().NotBeNull();
        staffProfile!.DepartmentRole.Should().Be(Department.IT);
    }

    [Fact]
    public async Task SeedAdminAsync_ShouldNotCreateDuplicateAdmin_WhenAdminAlreadyExists()
    {
        SetupDefaultSuccessBehavior();

        var existingAdmin = new UserBuilder()
            .WithRole(SystemRole.SuperAdmin)
            .WithEmail("existing.admin@omnimart.com")
            .Build();

        await _dbContext.Users.AddAsync(existingAdmin);
        await _dbContext.SaveChangesAsync();

        await _seeder.SeedAdminAsync();

        var adminsCount = await _dbContext.Users.CountAsync(u => u.Role == SystemRole.SuperAdmin);
        adminsCount.Should().Be(1);
    }

    [Fact]
    public async Task SeedAdminAsync_ShouldUseFallbackPassword_WhenConfigurationPasswordIsEmpty()
    {
        SetupDefaultSuccessBehavior();

        _configurationMock.Setup(x => x["AdminSettings:DefaultPassword"]).Returns(string.Empty);

        await _seeder.SeedAdminAsync();

        _passwordHasherMock.Verify(x => x.HashPassword("FallbackStrongPassword@2026!"), Times.Once);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}