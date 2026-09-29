using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Dashboard.Queries;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using OmniMart.Tests.Builders;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Dashboard.Queries;

public class GetAdminDashboardStatsQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly GetAdminDashboardStatsQueryHandler _handler;

    public GetAdminDashboardStatsQueryHandlerTests()
    {
        _handler = new GetAdminDashboardStatsQueryHandler(_contextMock.Object, _currentUserServiceMock.Object);
    }

    #region Helper Methods

    private void SetupDefaultCurrentUser()
    {
        _currentUserServiceMock.Setup(u => u.Role).Returns(SystemRole.SuperAdmin.ToString());
    }

    private void SetupUsers(params User[] users)
    {
        var mockDbSet = users.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.Users).Returns(mockDbSet.Object);
    }

    private void SetupVendors(params VendorProfile[] vendors)
    {
        var mockDbSet = vendors.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.VendorProfiles).Returns(mockDbSet.Object);
    }

    private void SetupProducts(params Product[] products)
    {
        var mockDbSet = products.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.Products).Returns(mockDbSet.Object);
    }

    private void SetupOrders(params Order[] orders)
    {
        var mockDbSet = orders.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.Orders).Returns(mockDbSet.Object);
    }

    #endregion

    #region Tests

    [Theory]
    [InlineData(SystemRole.Customer)]
    [InlineData(SystemRole.Vendor)]
    [InlineData(SystemRole.Manager)]
    [InlineData(SystemRole.SupportAgent)]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIsNotAdminOrSuperAdmin(SystemRole role)
    {
        _currentUserServiceMock.Setup(u => u.Role).Returns(role.ToString());

        var query = new GetAdminDashboardStatsQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldReturnAccurateStats_WhenDataExists()
    {
        SetupDefaultCurrentUser();

        var activeCustomer = new UserBuilder().WithRole(SystemRole.Customer).Build();
        var inactiveCustomer = new UserBuilder().WithRole(SystemRole.Customer).Build();
        inactiveCustomer.Suspend("Violation");

        var activeVendorUser = new UserBuilder().WithRole(SystemRole.Vendor).Build();

        SetupUsers(activeCustomer, inactiveCustomer, activeVendorUser);

        var pendingVendor = new VendorProfileBuilder().Build();
        var approvedVendor = new VendorProfileBuilder().AsApproved().Build();

        SetupVendors(pendingVendor, approvedVendor);

        var pendingProduct = new ProductBuilder().WithStatus(ProductStatus.PendingReview).Build();
        var activeProduct = new ProductBuilder().WithStatus(ProductStatus.Active).Build();
        var deletedPendingProduct = new ProductBuilder().WithStatus(ProductStatus.PendingReview).AsDeleted().Build();

        SetupProducts(pendingProduct, activeProduct, deletedPendingProduct);

        var deliveredOrder = new OrderBuilder().WithStatus(OrderStatus.Delivered).WithForcedTotalAmount(500m).Build();
        var pendingOrder = new OrderBuilder().WithStatus(OrderStatus.Pending).WithForcedTotalAmount(300m).Build();

        SetupOrders(deliveredOrder, pendingOrder);

        var query = new GetAdminDashboardStatsQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var stats = result.Value!;

        stats.TotalActiveCustomers.Should().Be(1);
        stats.TotalActiveVendors.Should().Be(1);

        stats.PendingVendorApprovals.Should().Be(1);
        stats.PendingProductApprovals.Should().Be(1);

        stats.TotalOrders.Should().Be(2);
        stats.TotalPlatformRevenue.Should().Be(500m);
    }

    #endregion
}