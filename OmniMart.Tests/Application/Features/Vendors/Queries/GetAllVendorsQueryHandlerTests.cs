using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Vendors.Queries;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using OmniMart.Tests.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Vendors.Queries;

public class GetAllVendorsQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ILogger<GetAllVendorsQueryHandler>> _loggerMock = new();
    private readonly GetAllVendorsQueryHandler _handler;
    private readonly Guid _defaultUserId = Guid.NewGuid();

    public GetAllVendorsQueryHandlerTests()
    {
        _handler = new GetAllVendorsQueryHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);
    }

    #region Helper Methods 

    private void SetupCurrentUser()
    {
        _currentUserServiceMock.Setup(s => s.Role).Returns(SystemRole.Admin.ToString());
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_defaultUserId.ToString());
    }

    private void SetupVendorsDb(params VendorProfile[] vendors)
    {
        var mockDbSet = vendors.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.VendorProfiles).Returns(mockDbSet.Object);
    }

    #endregion

    #region Tests

    [Theory]
    [InlineData(SystemRole.Customer)]
    [InlineData(SystemRole.Vendor)]
    [InlineData(SystemRole.Manager)]
    [InlineData(SystemRole.SupportAgent)]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIsNotAdminOrSuperAdmin(SystemRole unauthorizedRole)
    {
        _currentUserServiceMock.Setup(s => s.Role).Returns(unauthorizedRole.ToString());
        var query = new GetAllVendorsQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("Only Administrators can view full vendor records");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyPaginatedResult_WhenNoApprovedVendorsExist()
    {
        SetupCurrentUser();

        var pendingVendor = new VendorProfileBuilder().Build();
        SetupVendorsDb(pendingVendor);

        var query = new GetAllVendorsQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.TotalCount.Should().Be(0);
        result.Value.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnFilteredVendors_WhenSearchTermIsProvided()
    {
        _currentUserServiceMock.Setup(s => s.Role).Returns(SystemRole.SuperAdmin.ToString());

        var user1 = new UserBuilder().WithFirstName("Ahmed").Build();
        var vendor1 = new VendorProfileBuilder()
            .WithUser(user1)
            .WithStoreName("Tech Store")
            .AsApproved()
            .Build();

        var user2 = new UserBuilder().WithFirstName("Ali").Build();
        var vendor2 = new VendorProfileBuilder()
            .WithUser(user2)
            .WithStoreName("Fashion Hub")
            .AsApproved()
            .Build();

        SetupVendorsDb(vendor1, vendor2);

        var query = new GetAllVendorsQuery(SearchTerm: "tech");

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Data.Should().HaveCount(1);
        result.Value.Data.First().StoreName.Should().Be("Tech Store");
    }

    [Fact]
    public async Task Handle_ShouldReturnFilteredVendors_WhenIsActiveFilterIsProvided()
    {
        SetupCurrentUser();

        var activeUser = new UserBuilder().Build(); 
        var activeVendor = new VendorProfileBuilder()
            .WithUser(activeUser)
            .WithStoreName("Active Store")
            .AsApproved()
            .Build();

        var inactiveUser = new UserBuilder().Build();
        inactiveUser.Suspend("Violation");
        var inactiveVendor = new VendorProfileBuilder()
            .WithUser(inactiveUser)
            .WithStoreName("Inactive Store")
            .AsApproved()
            .Build();

        SetupVendorsDb(activeVendor, inactiveVendor);

        var query = new GetAllVendorsQuery(IsActiveFilter: false);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Data.Should().HaveCount(1);
        result.Value.Data.First().StoreName.Should().Be("Inactive Store");
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedDtoAndPaginatedData_WhenVendorsExist()
    {
        SetupCurrentUser();

        var user1 = new UserBuilder()
            .WithFirstName("Omar")
            .WithLastName("Khaled")
            .WithEmail("omar@test.com")
            .Build();

        var vendor1 = new VendorProfileBuilder()
            .WithUser(user1)
            .WithStoreName("Electro World")
            .WithCommissionRate(10m)
            .AsApproved()
            .Build();

        var user2 = new UserBuilder().WithFirstName("Zaid").Build();
        var vendor2 = new VendorProfileBuilder().WithUser(user2).AsApproved().Build();

        SetupVendorsDb(vendor1, vendor2);

        var query = new GetAllVendorsQuery(Page: 1, PageSize: 1);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.Data.Should().HaveCount(1);

        var dto = result.Value.Data.First();
        dto.StoreName.Should().NotBeNullOrEmpty();
        dto.OwnerFullName.Should().Contain(dto.StoreName == "Electro World" ? "Omar" : "Zaid");
        dto.WalletBalance.Should().Be(0m); 
    }

    #endregion
}