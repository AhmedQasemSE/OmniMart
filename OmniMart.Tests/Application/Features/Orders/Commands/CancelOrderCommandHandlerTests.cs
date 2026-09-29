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

public class CancelOrderCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IAppDbContext> _dbContextMock = new();
    private readonly Mock<ILogger<CancelOrderCommandHandler>> _loggerMock = new();

    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<ICustomerProfileRepository> _customerRepoMock = new();
    private readonly Mock<IVendorProfileRepository> _vendorRepoMock = new();

    private readonly CancelOrderCommandHandler _handler;

    private readonly Guid _defaultUserId = Guid.NewGuid();
    private readonly Guid _defaultCustomerId = Guid.NewGuid();
    private readonly Guid _defaultVendorId = Guid.NewGuid();
    private readonly ProductVariant _defaultVariant;
    private readonly Order _defaultOrder;
    private readonly VendorProfile _defaultVendorProfile;
    private readonly CancelOrderCommand _defaultCommand;

    public CancelOrderCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Orders).Returns(_orderRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.CustomerProfiles).Returns(_customerRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.VendorProfiles).Returns(_vendorRepoMock.Object);

        _handler = new CancelOrderCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _dbContextMock.Object,
            _currentUserServiceMock.Object);

        _defaultVariant = new ProductVariant(Guid.NewGuid(), "SKU-123", 500m, 10);
        _defaultVariant.ReserveStock(4); 

        _defaultOrder = new OrderBuilder()
            .WithCustomerId(_defaultCustomerId)
            .WithVendorId(_defaultVendorId)
            .WithStatus(OrderStatus.Pending)
            .WithItem(_defaultVariant.Id, 500m, 4, _defaultVariant)
            .Build();

        _defaultVendorProfile = new VendorProfileBuilder().WithCurrentBalance(5000m).Build();

        _defaultCommand = new CancelOrderCommand(_defaultOrder.Id, new byte[] { 1, 2, 3 });
    }

    #region Helper Methods

    private void SetupDefaultSuccessBehavior()
    {
        _currentUserServiceMock.Setup(u => u.UserId).Returns(_defaultUserId.ToString());
        _currentUserServiceMock.Setup(u => u.Role).Returns(SystemRole.Admin.ToString());

        _orderRepoMock.Setup(r => r.GetOrderWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(_defaultOrder);

        _vendorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(_defaultVendorProfile);
    }

    #endregion

    #region 1. Authorization & Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-valid-guid")]
    public async Task Handle_WhenUserIdIsInvalid_ShouldReturnUnauthorized(string? invalidUserId)
    {
        SetupDefaultSuccessBehavior();
        _currentUserServiceMock.Setup(u => u.UserId).Returns(invalidUserId);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("Unauthorized.");
    }

    [Fact]
    public async Task Handle_WhenOrderIsNull_ShouldReturnNotFound()
    {
        SetupDefaultSuccessBehavior();
        _orderRepoMock.Setup(r => r.GetOrderWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync((Order?)null);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("Order not found");
    }

    [Fact]
    public async Task Handle_WhenCustomerTriesToCancelAnotherCustomersOrder_ShouldReturnUnauthorized()
    {
        SetupDefaultSuccessBehavior();
        _currentUserServiceMock.Setup(u => u.Role).Returns(SystemRole.Customer.ToString());

        _customerRepoMock.Setup(r => r.GetCustomerIdByUserIdAsync(_defaultUserId, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(Guid.NewGuid());

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("You can only cancel your own orders.");
    }

    [Fact]
    public async Task Handle_WhenVendorTriesToCancelAnotherVendorsOrder_ShouldReturnUnauthorized()
    {
        SetupDefaultSuccessBehavior();
        _currentUserServiceMock.Setup(u => u.Role).Returns(SystemRole.Vendor.ToString());

        _vendorRepoMock.Setup(r => r.GetVendorIdByUserIdAsync(_defaultUserId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(Guid.NewGuid());

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("do not have permission");
    }

    [Fact]
    public async Task Handle_WhenOrderIsAlreadyCancelled_ShouldReturnValidationFailure()
    {
        SetupDefaultSuccessBehavior();

        var cancelledOrder = new OrderBuilder().WithStatus(OrderStatus.Cancelled).Build();
        _orderRepoMock.Setup(r => r.GetOrderWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(cancelledOrder);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.ErrorMessage.Should().Contain("Cannot change the status");
    }

    [Fact]
    public async Task Handle_WhenConcurrencyExceptionThrown_ShouldReturnConflict()
    {
        SetupDefaultSuccessBehavior();
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new DbUpdateConcurrencyException());

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("modified by another user");
    }

    #endregion

    #region 2. Success Scenarios 

    [Fact]
    public async Task Handle_WhenOrderIsNotPaid_ShouldCancelAndReleaseReservedStock()
    {
        SetupDefaultSuccessBehavior();

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _defaultOrder.Status.Should().Be(OrderStatus.Cancelled);
        _defaultVariant.ReservedStock.Should().Be(0);

        _vendorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrderIsPaid_ShouldCancelIncreaseStockAndDeductVendorBalance()
    {
        SetupDefaultSuccessBehavior();

        var paidVariant = new ProductVariant(Guid.NewGuid(), "SKU-PAID", 500m, 10); 
        var paidOrder = new OrderBuilder()
            .WithVendorId(_defaultVendorId)
            .WithStatus(OrderStatus.Processing)
            .WithForcedTotalAmount(2000m)
            .WithItem(paidVariant.Id, 500m, 4, paidVariant)
            .Build();

        _orderRepoMock.Setup(r => r.GetOrderWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(paidOrder);

        var command = new CancelOrderCommand(paidOrder.Id, new byte[] { 1, 2, 3 });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        paidOrder.Status.Should().Be(OrderStatus.Cancelled);

        paidVariant.StockQuantity.Should().Be(14);
        _defaultVendorProfile.CurrentBalance.Should().Be(3200m);
        _vendorRepoMock.Verify(r => r.GetByIdAsync(_defaultVendorId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrderIsPaidButVendorIsNull_ShouldStillCancelSuccessfully()
    {
        SetupDefaultSuccessBehavior();

        var paidOrder = new OrderBuilder()
            .WithVendorId(_defaultVendorId)
            .WithStatus(OrderStatus.Shipped)
            .Build();

        _orderRepoMock.Setup(r => r.GetOrderWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(paidOrder);

        _vendorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync((VendorProfile?)null);

        var command = new CancelOrderCommand(paidOrder.Id, new byte[] { 1, 2, 3 });
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProductVariantIsNull_ShouldSkipStockAndCancelSuccessfully()
    {
        SetupDefaultSuccessBehavior();

        var orderWithNoVariant = new OrderBuilder()
            .WithStatus(OrderStatus.Pending)
            .WithItem(Guid.NewGuid(), 500, 4) 
            .Build();

        _orderRepoMock.Setup(r => r.GetOrderWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(orderWithNoVariant);

        var command = new CancelOrderCommand(orderWithNoVariant.Id, new byte[] { 1, 2, 3 });
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        orderWithNoVariant.Status.Should().Be(OrderStatus.Cancelled);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}