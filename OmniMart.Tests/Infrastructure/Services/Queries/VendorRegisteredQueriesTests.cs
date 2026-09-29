using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using OmniMart.Infrastructure.Services.Queries;
using OmniMart.Tests.Builders;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Infrastructure.Services.Queries;

public class VendorRegisteredQueriesTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly VendorRegisteredQueries _queries;

    public VendorRegisteredQueriesTests()
    {
        _queries = new VendorRegisteredQueries(_contextMock.Object);
    }

    #region Helper Methods

    private void SetupVendors(params VendorProfile[] vendors)
    {
        var mockDbSet = vendors.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.VendorProfiles).Returns(mockDbSet.Object);
    }

    private void SetupUsers(params User[] users)
    {
        var mockDbSet = users.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.Users).Returns(mockDbSet.Object);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task GetVendorRegistrationDetailsAsync_WhenVendorExists_ShouldReturnDetailsAndAdminEmails()
    {
        var vendorUser = new UserBuilder().WithEmail("vendor@omnimart.com").Build();
        var vendorProfile = new VendorProfileBuilder().WithUser(vendorUser).WithStoreName("New Store").Build();

        var adminUser = new UserBuilder().WithRole(SystemRole.Admin).WithEmail("admin@omnimart.com").Build();

        SetupVendors(vendorProfile);
        SetupUsers(vendorUser, adminUser);

        var result = await _queries.GetVendorRegistrationDetailsAsync(vendorProfile.Id);

        result.Should().NotBeNull();
        result!.VendorEmail.Should().Be("vendor@omnimart.com");
        result.StoreName.Should().Be("New Store");
        result.StaffEmails.Should().Contain("admin@omnimart.com");
    }

    [Fact]
    public async Task GetVendorRegistrationDetailsAsync_WhenVendorDoesNotExist_ShouldReturnNull()
    {
        SetupVendors();

        var result = await _queries.GetVendorRegistrationDetailsAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    #endregion
}