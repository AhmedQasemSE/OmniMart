using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Dashboard.Queries;
using OmniMart.Controllers;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Controllers;

public class DashboardControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _controller = new DashboardController(_mediatorMock.Object);
    }

    #region Admin Dashboard Stats Tests

    [Fact]
    public async Task GetAdminDashboardStats_WhenSuccessful_ShouldReturnOkWithStats()
    {
        var expectedStats = new AdminDashboardStatsDto(
            TotalPlatformRevenue: 150000m,
            PendingVendorApprovals: 5,
            PendingProductApprovals: 12,
            TotalActiveCustomers: 300,
            TotalActiveVendors: 45,
            TotalOrders: 850
        );

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAdminDashboardStatsQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<AdminDashboardStatsDto>.Success(expectedStats));

        var result = await _controller.GetAdminDashboardStats(CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedStats);
    }

    [Fact]
    public async Task GetAdminDashboardStats_WhenUnauthorized_ShouldReturnUnauthorized401()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAdminDashboardStatsQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<AdminDashboardStatsDto>.Failure("Unauthorized", ErrorType.Unauthorized));

        var result = await _controller.GetAdminDashboardStats(CancellationToken.None);

        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult.Should().NotBeNull();
        unauthorizedResult!.StatusCode.Should().Be(401);
    }

    #endregion
}