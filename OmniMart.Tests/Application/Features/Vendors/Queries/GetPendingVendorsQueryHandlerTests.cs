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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Vendors.Queries;

public class GetPendingVendorsQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ILogger<GetPendingVendorsQueryHandler>> _loggerMock = new();
    private readonly GetPendingVendorsQueryHandler _handler;
    private readonly Guid _defaultUserId = Guid.NewGuid();
    public GetPendingVendorsQueryHandlerTests()
    {
        _handler = new GetPendingVendorsQueryHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);
    }

    #region Helper Methods

    private void SetupCurrentUser()
    {
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_defaultUserId.ToString());
        _currentUserServiceMock.Setup(s => s.Role).Returns(SystemRole.Admin.ToString()); 
    }

    private void SetupVendorsDb(params VendorProfile[] vendors)
    {
        var mockDbSet = vendors.BuildMockDbSet();
        _contextMock.Setup(c => c.VendorProfiles).Returns(mockDbSet.Object);
    }

    #endregion

    #region Tests

    [Theory]
    [InlineData(SystemRole.Customer)]
    [InlineData(SystemRole.Vendor)]
    [InlineData(SystemRole.SupportAgent)]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIsNotAdminOrSuperAdmin(SystemRole unauthorizedRole)
    {
        _currentUserServiceMock.Setup(s => s.Role).Returns(unauthorizedRole.ToString());
        var query = new GetPendingVendorsQuery();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("Only Administrators can view pending vendor registrations");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyPaginatedResult_WhenNoPendingVendorsExist()
    {
        SetupCurrentUser();

        var approvedVendor = new VendorProfileBuilder().AsApproved().Build();
        SetupVendorsDb(approvedVendor);

        var query = new GetPendingVendorsQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(0);
        result.Value.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedDtoAndPaginatedData_WhenPendingVendorsExist()
    {
        _currentUserServiceMock.Setup(s => s.Role).Returns(SystemRole.SuperAdmin.ToString());

        var user1 = new UserBuilder().WithFirstName("PendingOwner").Build();
        var pendingVendor = new VendorProfileBuilder()
            .WithUser(user1)
            .WithStoreName("Pending Store")
            .Build(); 

        var user2 = new UserBuilder().WithFirstName("ApprovedOwner").Build();
        var approvedVendor = new VendorProfileBuilder()
            .WithUser(user2)
            .WithStoreName("Active Store")
            .AsApproved()
            .Build();

        SetupVendorsDb(pendingVendor, approvedVendor);

        var query = new GetPendingVendorsQuery(Page: 1, PageSize: 10);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
        result.Value.Data.Should().HaveCount(1);

        var dto = result.Value.Data.First();
        dto.StoreName.Should().Be("Pending Store");
        dto.OwnerName.Should().Contain("PendingOwner");
    }

    #endregion
}