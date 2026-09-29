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

public class DeliverOrderCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ILogger<DeliverOrderCommandHandler>> _loggerMock = new();

    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IVendorProfileRepository> _vendorRepoMock = new();
    private readonly Mock<ICustomerProfileRepository> _customerRepoMock = new();

    private readonly DeliverOrderCommandHandler _handler;

    private readonly Guid _defaultUserId = Guid.NewGuid();
    private readonly Guid _defaultVendorId = Guid.NewGuid();
    private readonly Guid _defaultCustomerId = Guid.NewGuid();

    private readonly CustomerProfile _defaultCustomer;
    private readonly Order _defaultOrder;
    private readonly DeliverOrderCommand _defaultCommand;
    private readonly byte[] _defaultRowVersion = new byte[] { 1, 2, 3 };

    public DeliverOrderCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Orders).Returns(_orderRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.VendorProfiles).Returns(_vendorRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.CustomerProfiles).Returns(_customerRepoMock.Object);

        _handler = new DeliverOrderCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _currentUserServiceMock.Object);

        _defaultCustomer = new CustomerProfileBuilder()
            .WithId(_defaultCustomerId)
            .Build();

        var variant = new ProductVariantBuilder().WithPrice(150m).Build();

        _defaultOrder = new OrderBuilder()
            .WithCustomerId(_defaultCustomerId)
            .WithVendorId(_defaultVendorId)
            .WithStatus(OrderStatus.Shipped)
            .WithItem(variant.Id, 150m, 1, variant)
            .Build();

        _defaultCommand = new DeliverOrderCommand(_defaultOrder.Id, _defaultRowVersion);
    }

    #region Helper Methods

    private void SetupDefaultSuccessBehavior()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_defaultUserId.ToString());
        _currentUserServiceMock.Setup(x => x.Role).Returns(SystemRole.Vendor.ToString());

        _orderRepoMock.Setup(x => x.GetOrderWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_defaultOrder);

        _vendorRepoMock.Setup(x => x.GetVendorIdByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_defaultVendorId);

        _customerRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_defaultCustomer);
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
    public async Task Handle_ShouldReturnNotFound_WhenUserIsVendorAndProfileDoesNotExist()
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
    public async Task Handle_ShouldReturnUnauthorized_WhenOrderBelongsToDifferentVendor()
    {
        SetupDefaultSuccessBehavior();
        _vendorRepoMock.Setup(x => x.GetVendorIdByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("do not have permission");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationFailure_WhenDomainThrowsException()
    {
        SetupDefaultSuccessBehavior();

        var cancelledOrder = new OrderBuilder()
            .WithVendorId(_defaultVendorId) 
            .WithStatus(OrderStatus.Cancelled)
            .Build();

        _orderRepoMock.Setup(x => x.GetOrderWithDetailsAsync(cancelledOrder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cancelledOrder);

        var command = new DeliverOrderCommand(cancelledOrder.Id, _defaultRowVersion);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.ErrorMessage.Should().Contain("Cannot change the status"); 
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenConcurrencyExceptionIsThrown()
    {
        SetupDefaultSuccessBehavior();
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("modified by another user");
    }

    #endregion

    #region 2. Success Tests

    [Fact]
    public async Task Handle_ShouldDeliverOrder_WhenUserIsVendor_AndCustomerIsNull()
    {
        SetupDefaultSuccessBehavior();

        _customerRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerProfile?)null);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _defaultOrder.Status.Should().Be(OrderStatus.Delivered);

        _orderRepoMock.Verify(x => x.SetOriginalRowVersion(_defaultOrder, _defaultRowVersion), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldDeliverOrderAndAddPoints_WhenUserIsVendor_AndCustomerExists()
    {
        SetupDefaultSuccessBehavior();

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _defaultOrder.Status.Should().Be(OrderStatus.Delivered);

        _defaultCustomer.TotalSpent.Should().Be(_defaultOrder.TotalAmount);

        _orderRepoMock.Verify(x => x.SetOriginalRowVersion(_defaultOrder, _defaultRowVersion), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldDeliverOrderAndAddPoints_WhenUserIsAdmin()
    {
        SetupDefaultSuccessBehavior();

        _currentUserServiceMock.Setup(x => x.Role).Returns(SystemRole.Admin.ToString());

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _defaultOrder.Status.Should().Be(OrderStatus.Delivered);

        _vendorRepoMock.Verify(x => x.GetVendorIdByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        _orderRepoMock.Verify(x => x.SetOriginalRowVersion(_defaultOrder, _defaultRowVersion), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}