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

public class VendorRequiresReapprovalQueriesTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly VendorRequiresReapprovalQueries _queries;

    public VendorRequiresReapprovalQueriesTests()
    {
        _queries = new VendorRequiresReapprovalQueries(_contextMock.Object);
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
    public async Task GetVendorAndStaffDetailsAsync_WhenVendorExists_ShouldReturnStoreNameAndStaffEmails()
    {
        var vendorProfile = new VendorProfileBuilder().WithStoreName("Update Store").Build();
        var adminUser = new UserBuilder().WithRole(SystemRole.Admin).WithEmail("admin@omnimart.com").Build();

        SetupVendors(vendorProfile);
        SetupUsers(adminUser);

        var result = await _queries.GetVendorAndStaffDetailsAsync(vendorProfile.Id);

        result.Should().NotBeNull();
        result!.StoreName.Should().Be("Update Store");
        result.StaffEmails.Should().Contain("admin@omnimart.com");
    }

    [Fact]
    public async Task GetVendorAndStaffDetailsAsync_WhenVendorDoesNotExist_ShouldReturnNull()
    {
        SetupVendors();

        var result = await _queries.GetVendorAndStaffDetailsAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    #endregion
}