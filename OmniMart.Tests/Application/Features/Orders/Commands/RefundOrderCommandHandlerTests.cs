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

public class RefundOrderCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ILogger<RefundOrderCommandHandler>> _loggerMock = new();

    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IVendorProfileRepository> _vendorRepoMock = new();
    private readonly Mock<ICustomerProfileRepository> _customerRepoMock = new();

    private readonly RefundOrderCommandHandler _handler;

    private readonly Guid _defaultUserId = Guid.NewGuid();
    private readonly Guid _defaultVendorId = Guid.NewGuid();
    private readonly Guid _defaultCustomerId = Guid.NewGuid();

    private readonly CustomerProfile _defaultCustomer;
    private readonly VendorProfile _defaultVendor;
    private readonly Order _defaultPaidOrder;
    private readonly ProductVariant _defaultVariant;
    private readonly RefundOrderCommand _defaultCommand;
    private readonly byte[] _defaultRowVersion = new byte[] { 1, 2, 3 };

    public RefundOrderCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Orders).Returns(_orderRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.VendorProfiles).Returns(_vendorRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.CustomerProfiles).Returns(_customerRepoMock.Object);

        _handler = new RefundOrderCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _currentUserServiceMock.Object);

        _defaultCustomer = new CustomerProfileBuilder()
            .WithId(_defaultCustomerId)
            .Build();
        _defaultCustomer.RecordPurchase(2000m); 

        _defaultVendor = new VendorProfileBuilder()
            .WithId(_defaultVendorId)
            .WithCurrentBalance(5000m)
            .Build();

        _defaultVariant = new ProductVariantBuilder()
            .WithPrice(500m)
            .WithStockQuantity(10)
            .Build();

        _defaultPaidOrder = new OrderBuilder()
            .WithCustomerId(_defaultCustomerId)
            .WithVendorId(_defaultVendorId)
            .WithStatus(OrderStatus.Processing)
            .WithItem(_defaultVariant.Id, 500m, 2, _defaultVariant)
            .Build();

        _defaultCommand = new RefundOrderCommand(_defaultPaidOrder.Id, _defaultRowVersion);
    }

    #region Helper Methods

    private void SetupDefaultSuccessBehavior()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_defaultUserId.ToString());
        _currentUserServiceMock.Setup(x => x.Role).Returns(SystemRole.Admin.ToString());

        _orderRepoMock.Setup(x => x.GetOrderWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_defaultPaidOrder);

        _vendorRepoMock.Setup(x => x.GetVendorIdByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_defaultVendorId);

        _vendorRepoMock.Setup(x => x.GetByIdAsync(_defaultVendorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_defaultVendor);

        _customerRepoMock.Setup(x => x.GetByIdAsync(_defaultCustomerId, It.IsAny<CancellationToken>()))
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
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenUserIsVendorAndProfileDoesNotExist()
    {
        SetupDefaultSuccessBehavior();
        _currentUserServiceMock.Setup(x => x.Role).Returns(SystemRole.Vendor.ToString());

        _vendorRepoMock.Setup(x => x.GetVendorIdByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenOrderBelongsToDifferentVendor()
    {
        SetupDefaultSuccessBehavior();
        _currentUserServiceMock.Setup(x => x.Role).Returns(SystemRole.Vendor.ToString());

        _vendorRepoMock.Setup(x => x.GetVendorIdByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationFailure_WhenDomainThrowsException()
    {
        SetupDefaultSuccessBehavior();

        var refundedOrder = new OrderBuilder()
            .WithVendorId(_defaultVendorId)
            .WithStatus(OrderStatus.Refunded)
            .Build();

        _orderRepoMock.Setup(x => x.GetOrderWithDetailsAsync(refundedOrder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refundedOrder);

        var command = new RefundOrderCommand(refundedOrder.Id, _defaultRowVersion);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
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
    }

    #endregion

    #region 2. Success Tests (Paid vs Unpaid Scenarios)

    [Fact]
    public async Task Handle_ShouldRefundPaidOrder_IncreaseStock_AndDeductBalances()
    {
        SetupDefaultSuccessBehavior();
        var initialCustomerPoints = _defaultCustomer.LoyaltyPoints;

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _defaultPaidOrder.Status.Should().Be(OrderStatus.Refunded);

        _defaultVariant.StockQuantity.Should().Be(12);

        _defaultVendor.CurrentBalance.Should().BeLessThan(5000m);

        _defaultCustomer.LoyaltyPoints.Should().BeLessThan(initialCustomerPoints);

        _vendorRepoMock.Verify(x => x.GetByIdAsync(_defaultVendorId, It.IsAny<CancellationToken>()), Times.Once);
        _customerRepoMock.Verify(x => x.GetByIdAsync(_defaultCustomerId, It.IsAny<CancellationToken>()), Times.Once);

        _orderRepoMock.Verify(x => x.SetOriginalRowVersion(_defaultPaidOrder, _defaultRowVersion), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRefundUnpaidOrder_IncreaseStock_ButSkipBalanceDeductions()
    {
        SetupDefaultSuccessBehavior();

        var unpaidOrder = new OrderBuilder()
            .WithCustomerId(_defaultCustomerId)
            .WithVendorId(_defaultVendorId)
            .WithStatus(OrderStatus.Pending)
            .WithItem(_defaultVariant.Id, 500m, 2, _defaultVariant)
            .Build();

        _orderRepoMock.Setup(x => x.GetOrderWithDetailsAsync(unpaidOrder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(unpaidOrder);

        var command = new RefundOrderCommand(unpaidOrder.Id, _defaultRowVersion);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        unpaidOrder.Status.Should().Be(OrderStatus.Refunded);

        _defaultVariant.StockQuantity.Should().Be(12);

        _vendorRepoMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _customerRepoMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRefundPaidOrder_WhenUserIsVendor_AndOwnsTheOrder()
    {
        SetupDefaultSuccessBehavior();
        _currentUserServiceMock.Setup(x => x.Role).Returns(SystemRole.Vendor.ToString());

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _defaultPaidOrder.Status.Should().Be(OrderStatus.Refunded);

        _vendorRepoMock.Verify(x => x.GetVendorIdByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}