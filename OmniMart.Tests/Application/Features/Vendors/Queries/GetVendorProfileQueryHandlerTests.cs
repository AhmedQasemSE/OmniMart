using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Vendors.Queries;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Tests.Builders;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Vendors.Queries;

public class GetVendorProfileQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

    private readonly GetVendorProfileQueryHandler _handler;
    private readonly Guid _defaultUserId = Guid.NewGuid();
    public GetVendorProfileQueryHandlerTests()
    {
        _handler = new GetVendorProfileQueryHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object);
    }

    #region Helper Methods (Using params for clean and condition-free setup)

    private void SetupValidDependencies(params VendorProfile[] profiles)
    {
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_defaultUserId.ToString());

        var mockDbSet = profiles.BuildMockDbSet();
        _contextMock.Setup(c => c.VendorProfiles).Returns(mockDbSet.Object);
    }

    #endregion

    #region Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid-guid-string")]
    public async Task Handle_ShouldReturnFailure_WhenUserIdIsInvalidOrUnauthorized(string? invalidUserId)
    {
        var query = new GetVendorProfileQuery();

        _currentUserServiceMock.Setup(s => s.UserId).Returns(invalidUserId);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenVendorProfileNotFound()
    {
        var query = new GetVendorProfileQuery();

        SetupValidDependencies();

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("Vendor profile not found");
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_AndMapDtoCorrectly_WhenProfileExists()
    {
        var query = new GetVendorProfileQuery();

        var vendorProfile = new VendorProfileBuilder()
            .WithId(Guid.NewGuid())
            .WithUserId(_defaultUserId)
            .WithStoreName("Super Tech Store")
            .WithCommissionRate(12.5m)
            .AsApproved()
            .Build();

        SetupValidDependencies(vendorProfile);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var dto = result.Value;
        dto.VendorId.Should().Be(vendorProfile.Id);
        dto.StoreName.Should().Be(vendorProfile.StoreName);
        dto.CommissionRate.Should().Be(vendorProfile.CommissionRate);
        dto.IsApproved.Should().BeTrue();
    }

    #endregion
}