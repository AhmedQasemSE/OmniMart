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
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Vendors.Commands;

public class ApproveVendorCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IVendorProfileRepository> _vendorRepoMock = new();
    private readonly Mock<ILogger<ApproveVendorCommandHandler>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();

    private readonly ApproveVendorCommandHandler _handler;
    private readonly Guid _defaultUserId = Guid.NewGuid();
    public ApproveVendorCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.VendorProfiles).Returns(_vendorRepoMock.Object);

        SetupCurrentUser();

        _handler = new ApproveVendorCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _currentUserServiceMock.Object);
    }

    #region Helper Methods

    private void SetupCurrentUser()
    {
        _currentUserServiceMock.Setup(s => s.UserId).Returns(_defaultUserId.ToString());
        _currentUserServiceMock.Setup(s => s.Role).Returns(SystemRole.Admin.ToString()); 
    }
    #endregion

    #region Part 1: Authorization & Validation Tests

    [Theory]
    [InlineData(SystemRole.Customer)]
    [InlineData(SystemRole.Vendor)]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAdmin(SystemRole unauthorizedRole)
    {
        var command = new ApproveVendorCommand(Guid.NewGuid());

        SetupCurrentUser();
        _currentUserServiceMock.Setup(s => s.Role).Returns(unauthorizedRole.ToString());


        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("Only Administrators can approve");

        _unitOfWorkMock.Verify(u => u.VendorProfiles.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenVendorProfileIsNotFound()
    {
        var command = new ApproveVendorCommand(Guid.NewGuid());

        _vendorRepoMock.Setup(r => r.GetByIdAsync(command.VendorId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((VendorProfile?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("Vendor profile not found");
    }

    #endregion

    #region Part 2: Business Logic & State Mutation Tests

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenVendorIsPendingApproval()
    {
        var vendorId = Guid.NewGuid();
        var command = new ApproveVendorCommand(vendorId);

        var pendingVendor = new VendorProfileBuilder()
            .WithId(vendorId)
            .Build();

        _vendorRepoMock.Setup(r => r.GetByIdAsync(vendorId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(pendingVendor);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        pendingVendor.IsApproved.Should().BeTrue();

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenVendorIsAlreadyApproved()
    {
        var vendorId = Guid.NewGuid();
        var command = new ApproveVendorCommand(vendorId);

        var approvedVendor = new VendorProfileBuilder()
            .WithId(vendorId)
            .AsApproved()
            .Build();

        _vendorRepoMock.Setup(r => r.GetByIdAsync(vendorId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(approvedVendor);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);

        result.ErrorMessage.Should().Contain("already approved");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}