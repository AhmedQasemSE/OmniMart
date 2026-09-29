using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Vendors.Commands;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Tests.Builders;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Vendors.Commands;

public class UpdateVendorCommissionRateCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IVendorProfileRepository> _vendorRepoMock = new();
    private readonly Mock<IStaffProfileRepository> _staffRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

    private readonly UpdateVendorCommissionRateCommandHandler _handler;
    private readonly Guid _defaultUserId = Guid.NewGuid();
    private readonly Guid _defaultVendorId = Guid.NewGuid();
    private readonly Guid _defaultStaffId = Guid.NewGuid();
    
    public UpdateVendorCommissionRateCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.VendorProfiles).Returns(_vendorRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.StaffProfiles).Returns(_staffRepoMock.Object);

        _handler = new UpdateVendorCommissionRateCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object);
    }

    #region Helper Methods (The Professional Actor/Target Split)

    private void SetupCurrentUser()
    {
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_defaultUserId.ToString());
    }

    private void SetupAdminProfile()
    {
        _staffRepoMock.Setup(r => r.GetStaffIdByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(_defaultStaffId);
    }

    private void SetupTargetVendor(VendorProfile? vendor)
    {
        _vendorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                       .ReturnsAsync(vendor);
    }

    private UpdateVendorCommissionRateCommand CreateValidCommand()
    {
        return new UpdateVendorCommissionRateCommand(_defaultVendorId, 15m, new byte[] { 1, 2, 3, 4 });
    }

    #endregion

    #region Part 1: Authorization & Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid-guid")]
    public async Task Handle_ShouldReturnFailure_WhenUserIsUnauthorizedOrTokenInvalid(string? invalidUserId)
    {
        var command = CreateValidCommand();

        SetupCurrentUser();
        _currentUserServiceMock.Setup(s => s.UserId).Returns(invalidUserId);


        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenAdminProfileNotFound()
    {
        var command = CreateValidCommand();

        SetupCurrentUser(); 

        var vendor = new VendorProfileBuilder().WithId(_defaultVendorId).Build();
        SetupTargetVendor(vendor);

        _staffRepoMock.Setup(r => r.GetStaffIdByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((Guid?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("Admin profile not found");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenVendorProfileNotFound()
    {
        var command = CreateValidCommand();

        SetupCurrentUser();
        SetupAdminProfile();

        SetupTargetVendor(null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("Vendor profile not found");
    }

    #endregion

    #region Part 2: Business Logic & Concurrency Tests

    [Fact]
    public async Task Handle_ShouldReturnSuccess_AndUpdateRate_WhenDataIsValid()
    {
        var command = CreateValidCommand();

        SetupCurrentUser();
        SetupAdminProfile();

        var vendor = new VendorProfileBuilder()
            .WithId(_defaultVendorId)
            .WithCommissionRate(10m)
            .Build();
        SetupTargetVendor(vendor);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        vendor.CommissionRate.Should().Be(15m); 

        _vendorRepoMock.Verify(r => r.SetOriginalRowVersion(vendor, command.RowVersion), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenDbUpdateConcurrencyExceptionOccurs()
    {
        var command = CreateValidCommand();

        SetupCurrentUser();
        SetupAdminProfile();

        var vendor = new VendorProfileBuilder().WithId(_defaultVendorId).Build();
        SetupTargetVendor(vendor);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new DbUpdateConcurrencyException());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("Data discrepancy");
    }

    #endregion
}