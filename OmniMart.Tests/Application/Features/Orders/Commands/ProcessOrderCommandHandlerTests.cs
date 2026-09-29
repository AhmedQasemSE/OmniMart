using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Orders.Commands;
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

namespace OmniMart.Tests.Application.Features.Orders.Commands;

public class ProcessOrderCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ILogger<ProcessOrderCommandHandler>> _loggerMock = new();
    private readonly Mock<IAppDbContext> _contextMock = new();

    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IVendorProfileRepository> _vendorRepoMock = new();

    private readonly ProcessOrderCommandHandler _handler;

    private readonly Guid _defaultUserId = Guid.NewGuid();
    private readonly Guid _defaultVendorId = Guid.NewGuid();

    private readonly Order _defaultOrder;
    private readonly ProcessOrderCommand _defaultCommand;
    private readonly byte[] _defaultRowVersion = new byte[] { 1, 2, 3 };

    public ProcessOrderCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Orders).Returns(_orderRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.VendorProfiles).Returns(_vendorRepoMock.Object);

        _handler = new ProcessOrderCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _contextMock.Object,
            _currentUserServiceMock.Object);

        _defaultOrder = new OrderBuilder()
            .WithVendorId(_defaultVendorId)
            .WithStatus(OrderStatus.Pending)
            .Build();

        _defaultCommand = new ProcessOrderCommand(_defaultOrder.Id, _defaultRowVersion);
    }

    #region Helper Methods

    private void SetupDefaultSuccessBehavior()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_defaultUserId.ToString());

        _vendorRepoMock.Setup(x => x.GetVendorIdByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_defaultVendorId);

        _orderRepoMock.Setup(x => x.GetOrderWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_defaultOrder);
    }

    #endregion

    #region 1. Validation & Failure Tests

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIdIsInvalid()
    {
        SetupDefaultSuccessBehavior();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(string.Empty);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("Unauthorized");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenVendorProfileDoesNotExist()
    {
        SetupDefaultSuccessBehavior();
        _vendorRepoMock.Setup(x => x.GetVendorIdByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("Vendor profile not found");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        SetupDefaultSuccessBehavior();
        _orderRepoMock.Setup(x => x.GetOrderWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("Order not found");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenOrderBelongsToDifferentVendor()
    {
        SetupDefaultSuccessBehavior();
        _vendorRepoMock.Setup(x => x.GetVendorIdByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("You do not have permission");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationFailure_WhenDomainThrowsInvalidOperationException()
    {
        SetupDefaultSuccessBehavior();

        var cancelledOrder = new OrderBuilder()
            .WithVendorId(_defaultVendorId)
            .WithStatus(OrderStatus.Cancelled)
            .Build();

        _orderRepoMock.Setup(x => x.GetOrderWithDetailsAsync(cancelledOrder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cancelledOrder);

        var command = new ProcessOrderCommand(cancelledOrder.Id, _defaultRowVersion);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenDbUpdateConcurrencyExceptionIsThrown()
    {
        SetupDefaultSuccessBehavior();
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
    }

    #endregion

    #region 2. Success Tests

    [Fact]
    public async Task Handle_ShouldProcessOrderSuccessfully_WhenVendorIsOwner()
    {
        SetupDefaultSuccessBehavior();

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _defaultOrder.Status.Should().Be(OrderStatus.Processing);

        _orderRepoMock.Verify(x => x.SetOriginalRowVersion(_defaultOrder, _defaultRowVersion), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}