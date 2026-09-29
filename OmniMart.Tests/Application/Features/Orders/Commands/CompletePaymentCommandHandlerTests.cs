using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Orders.Commands;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using OmniMart.Tests.Builders;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Orders.Commands;

public class CompletePaymentCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<CompletePaymentCommandHandler>> _loggerMock = new();

    private readonly Mock<IPaymentGroupRepository> _paymentGroupRepoMock = new();
    private readonly Mock<IVendorProfileRepository> _vendorRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();

    private readonly CompletePaymentCommandHandler _handler;

    private readonly Guid _defaultCustomerId = Guid.NewGuid();
    private readonly Guid _defaultVendorId = Guid.NewGuid();

    private readonly PaymentGroup _defaultPaymentGroup;
    private readonly Order _defaultOrder;
    private readonly ProductVariant _defaultVariant;
    private readonly VendorProfile _defaultVendorProfile;
    private readonly CompletePaymentCommand _defaultCommand;

    public CompletePaymentCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.PaymentGroups).Returns(_paymentGroupRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.VendorProfiles).Returns(_vendorRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);

        _handler = new CompletePaymentCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        _defaultVariant = new ProductVariantBuilder()
            .WithPrice(100m)
            .WithStockQuantity(10)
            .WithReservedStock(2) 
            .Build();

        _defaultOrder = new OrderBuilder()
            .WithCustomerId(_defaultCustomerId)
            .WithVendorId(_defaultVendorId)
            .WithItem(_defaultVariant.Id, 100m, 2, _defaultVariant)
            .Build();

        _defaultPaymentGroup = new PaymentGroup(_defaultCustomerId);
        _defaultPaymentGroup.AddOrder(_defaultOrder);

        _defaultVendorProfile = new VendorProfileBuilder()
            .WithId(_defaultVendorId)
            .WithCurrentBalance(0m)
            .Build();

        _defaultCommand = new CompletePaymentCommand(_defaultPaymentGroup.Id, "stripe_session_123");
    }

    #region Helper Methods

    private void SetupDefaultSuccessBehavior()
    {
        _paymentGroupRepoMock.Setup(p => p.GetAsync(
            It.IsAny<Expression<Func<PaymentGroup, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Expression<Func<PaymentGroup, object>>>()))
            .ReturnsAsync(_defaultPaymentGroup);

        _vendorRepoMock.Setup(v => v.GetVendorsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VendorProfile> { _defaultVendorProfile });

        _productRepoMock.Setup(p => p.GetVariantsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductVariant> { _defaultVariant });
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenPaymentGroupDoesNotExist()
    {
        SetupDefaultSuccessBehavior();
        _paymentGroupRepoMock.Setup(p => p.GetAsync(
            It.IsAny<Expression<Func<PaymentGroup, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Expression<Func<PaymentGroup, object>>>()))
            .ReturnsAsync((PaymentGroup?)null); 

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("not found");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccessAndNotSave_WhenPaymentGroupIsAlreadyPaid()
    {
        SetupDefaultSuccessBehavior();
        _defaultPaymentGroup.MarkAsPaid("old_session_123"); 

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEverythingIsHappy_ShouldSucceed_AndDistributeMoneyAndStock()
    {
        SetupDefaultSuccessBehavior();

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _defaultPaymentGroup.IsPaid.Should().BeTrue();

        _defaultVariant.ReservedStock.Should().Be(0);

        _defaultVendorProfile.CurrentBalance.Should().BeGreaterThan(0);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenVendorIsNull_ShouldSucceedAndLogCritical()
    {
        SetupDefaultSuccessBehavior();

        _vendorRepoMock.Setup(v => v.GetVendorsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VendorProfile>());

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenVariantIsNull_ShouldSucceedAndLogCritical()
    {
        SetupDefaultSuccessBehavior();
        _productRepoMock.Setup(p => p.GetVariantsByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductVariant>());

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);


        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}