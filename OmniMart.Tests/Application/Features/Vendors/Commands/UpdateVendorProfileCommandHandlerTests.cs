using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Vendors.Commands;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using OmniMart.Tests.Builders;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Vendors.Commands;

public class UpdateVendorProfileCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IVendorProfileRepository> _vendorRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ILogger<UpdateVendorProfileCommandHandler>> _loggerMock = new();

    private readonly UpdateVendorProfileCommandHandler _handler;
    private readonly Guid _defaultUserId = Guid.NewGuid();

    public UpdateVendorProfileCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.VendorProfiles).Returns(_vendorRepoMock.Object);

        _handler = new UpdateVendorProfileCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _currentUserServiceMock.Object);
    }

    #region Helper Methods (The Clean Setup)

    private void SetupCurrentUser()
    {
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_defaultUserId.ToString());
        _currentUserServiceMock.Setup(s => s.Role).Returns(SystemRole.Vendor.ToString());
    }

    private void SetupTargetVendor(VendorProfile? vendorProfile)
    {
        _vendorRepoMock.Setup(r => r.GetAsync(It.IsAny<Expression<Func<VendorProfile, bool>>>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(vendorProfile);
    }

    private UpdateVendorProfileCommand CreateValidCommand()
    {
        return new UpdateVendorProfileCommand("New Store Name", "987654321");
    }

    #endregion

    #region Part 1: Authorization & Validation Tests

    [Theory]
    [InlineData("Customer")]
    [InlineData("Admin")]
    [InlineData("SuperAdmin")]
    [InlineData("")]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotVendor(string invalidRole)
    {
        SetupCurrentUser();

        _currentUserServiceMock.Setup(s => s.Role).Returns(invalidRole);

        var command = CreateValidCommand();
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("Only vendors can update");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid-guid-string")]
    public async Task Handle_ShouldReturnFailure_WhenUserIdIsInvalid(string? invalidUserId)
    {
        SetupCurrentUser();

        _currentUserServiceMock.Setup(s => s.UserId).Returns(invalidUserId);

        var command = CreateValidCommand();
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenVendorProfileNotFound()
    {
        SetupCurrentUser();
        SetupTargetVendor(null); 

        var command = CreateValidCommand();
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("Vendor profile not found");
    }

    #endregion

    #region Part 2: Business Logic & Exception Tests

    [Fact]
    public async Task Handle_ShouldReturnSuccess_AndUpdateProfile_WhenDataIsValid()
    {
        SetupCurrentUser();

        var vendor = new VendorProfileBuilder()
            .WithId(Guid.NewGuid())
            .WithUserId(_defaultUserId)
            .WithStoreName("Old Store")
            .Build();

        SetupTargetVendor(vendor);

        var command = CreateValidCommand();
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        vendor.StoreName.Should().Be(command.StoreName);
        vendor.CommercialRegisterNumber.Should().Be(command.CommercialRegisterNumber);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenExceptionOccursDuringSave()
    {
        SetupCurrentUser();

        var vendor = new VendorProfileBuilder().WithUserId(_defaultUserId).Build();
        SetupTargetVendor(vendor);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new Exception("Database connection failed"));

        var command = CreateValidCommand();
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("An error occurred while updating");
    }

    #endregion
}