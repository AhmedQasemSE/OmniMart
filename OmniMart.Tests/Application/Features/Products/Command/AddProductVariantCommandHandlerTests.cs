using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Products.Commands;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Tests.Builders;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Command;

public class AddProductVariantCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ILogger<AddProductVariantCommandHandler>> _loggerMock = new();

    private readonly Mock<IVendorProfileRepository> _vendorProfileRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();

    private readonly AddProductVariantCommandHandler _handler;

    private readonly Guid _defaultUserId = Guid.NewGuid();
    private readonly Guid _defaultVendorId = Guid.NewGuid();
    private readonly Guid _defaultProductId = Guid.NewGuid();

    private readonly Product _defaultProduct;
    private readonly AddProductVariantCommand _defaultCommand;

    public AddProductVariantCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.VendorProfiles).Returns(_vendorProfileRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);

        _handler = new AddProductVariantCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _currentUserServiceMock.Object);

        _defaultProduct = new ProductBuilder()
            .WithId(_defaultProductId)
            .WithVendorIds(_defaultVendorId, _defaultUserId)
            .Build();

        _defaultCommand = new AddProductVariantCommand(_defaultProductId, "SKU-999", 100m, 10);
    }

    #region Helper Methods

    private void SetupDefaultSuccessBehavior()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_defaultUserId.ToString());

        _vendorProfileRepoMock
            .Setup(x => x.GetVendorIdByUserIdAsync(_defaultUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_defaultVendorId);

        _productRepoMock
            .Setup(x => x.GetProductWithAllVariantsForUpdateAsync(_defaultProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_defaultProduct);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_WhenAllDataIsValid_ShouldAddVariantAndSave()
    {
        SetupDefaultSuccessBehavior();

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-valid-guid")]
    public async Task Handle_WhenUserIdIsInvalid_ShouldReturnUnauthorized(string? invalidUserId)
    {
        SetupDefaultSuccessBehavior();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(invalidUserId);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenVendorProfileNotFound_ShouldReturnNotFound()
    {
        SetupDefaultSuccessBehavior();
        _vendorProfileRepoMock
            .Setup(x => x.GetVendorIdByUserIdAsync(_defaultUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldReturnNotFound()
    {
        SetupDefaultSuccessBehavior();
        _productRepoMock
            .Setup(x => x.GetProductWithAllVariantsForUpdateAsync(_defaultProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotTheProductOwner_ShouldReturnUnauthorized()
    {
        SetupDefaultSuccessBehavior();

        var productOwnedByAnotherVendor = new ProductBuilder()
            .WithId(_defaultProductId)
            .WithVendorIds(Guid.NewGuid(), Guid.NewGuid())
            .Build();

        _productRepoMock
            .Setup(x => x.GetProductWithAllVariantsForUpdateAsync(_defaultProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(productOwnedByAnotherVendor);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("do not have permission");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDomainThrowsException_ShouldReturnValidationFailure()
    {
        SetupDefaultSuccessBehavior();

        var invalidCommand = new AddProductVariantCommand(_defaultProductId, "SKU-999", -50m, 10);

        var result = await _handler.Handle(invalidCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}