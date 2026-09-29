using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Products.Commands;
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

namespace OmniMart.Tests.Application.Features.Products.Command;

public class SetPublishStatusCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<SetPublishStatusCommandHandler>> _loggerMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;

    private readonly Mock<IVendorProfileRepository> _vendorProfileRepoMock;
    private readonly Mock<IProductRepository> _productRepoMock;

    private readonly SetPublishStatusCommandHandler _handler;

    private readonly Guid _validUserId = Guid.NewGuid();
    private readonly Guid _validProductId = Guid.NewGuid();

    public SetPublishStatusCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<SetPublishStatusCommandHandler>>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _vendorProfileRepoMock = new Mock<IVendorProfileRepository>();
        _productRepoMock = new Mock<IProductRepository>();

        _unitOfWorkMock.Setup(u => u.VendorProfiles).Returns(_vendorProfileRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);

        _handler = new SetPublishStatusCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _currentUserServiceMock.Object);
    }

    #region Helper Methods

    private SetPublishStatusCommand CreateValidCommand()
    {
        return new SetPublishStatusCommand(_validProductId,true);
    }

    private void SetupValidContext()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_validUserId.ToString());

        var happyProduct = new ProductBuilder()
            .WithId(_validProductId)
            .Build();

        _vendorProfileRepoMock
            .Setup(x => x.GetVendorIdByUserIdAsync(_validUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(happyProduct.VendorId);

        _productRepoMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(happyProduct);
    }

    #endregion

    #region Tests
    [Fact]
    public async Task Handle_WhenPublishingAnActiveProduct_ShouldSetStatusAndSave()
    {
        SetupValidContext();
        var command = new  SetPublishStatusCommand(_validProductId, false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-valid-guid")]
    public async Task Handle_WhenUserIdIsInvalid_ShouldReturnUnauthorized(string? invalidUserId)
    {
        SetupValidContext();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(invalidUserId);

        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task Handle_WhenVendorProfileNotFound_ShouldReturnNotFound()
    {
        SetupValidContext();
        _vendorProfileRepoMock
            .Setup(x => x.GetVendorIdByUserIdAsync(_validUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldReturnNotFound()
    {
        SetupValidContext();
        _productRepoMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null); 

        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenStatusIsAlreadyTheSame_ShouldReturnSuccessWithoutSaving()
    {
        SetupValidContext();

        var alreadyPublishedProduct = new ProductBuilder().WithId(_validProductId)
            .Build();


        _vendorProfileRepoMock
    .Setup(x => x.GetVendorIdByUserIdAsync(_validUserId, It.IsAny<CancellationToken>()))
    .ReturnsAsync(alreadyPublishedProduct.VendorId);

        _productRepoMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(alreadyPublishedProduct);

        alreadyPublishedProduct.SetPublishStatus(true);

        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(); 

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(ProductStatus.Draft)]
    [InlineData(ProductStatus.PendingReview)]
    [InlineData(ProductStatus.Rejected)]
    [InlineData(ProductStatus.Suspended)]
    public async Task Handle_WhenPublishingAndProductIsNotActive_ShouldReturnConflict(ProductStatus invalidStatus)
    {
        SetupValidContext();

        var invalidProduct = new ProductBuilder().WithId(_validProductId)
            .WithStatus(invalidStatus)
            .Build();

        _vendorProfileRepoMock
            .Setup(x => x.GetVendorIdByUserIdAsync(_validUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidProduct.VendorId);

        _productRepoMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidProduct);

        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("Only active products");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotTheProductOwner_ShouldReturnUnauthorized()
    {
        SetupValidContext();

        var productOwnedByAnotherVendor = new ProductBuilder().WithId(_validProductId)
            .Build();

        _vendorProfileRepoMock
     .Setup(x => x.GetVendorIdByUserIdAsync(_validUserId, It.IsAny<CancellationToken>()))
     .ReturnsAsync(Guid.NewGuid());

        _productRepoMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(productOwnedByAnotherVendor);

        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    #endregion
}