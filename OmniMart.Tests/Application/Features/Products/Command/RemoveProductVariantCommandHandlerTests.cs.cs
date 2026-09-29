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

public class RemoveProductVariantCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<RemoveProductVariantCommandHandler>> _loggerMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;

    private readonly Mock<IVendorProfileRepository> _vendorProfileRepoMock;
    private readonly Mock<IProductRepository> _productRepoMock;

    private readonly RemoveProductVariantCommandHandler _handler;

    private readonly Guid _validUserId = Guid.NewGuid();
    private readonly Guid _validProductId = Guid.NewGuid();
    private readonly string _validSku = "SKU-REMOVE-123";

    public RemoveProductVariantCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<RemoveProductVariantCommandHandler>>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _vendorProfileRepoMock = new Mock<IVendorProfileRepository>();
        _productRepoMock = new Mock<IProductRepository>();

        _unitOfWorkMock.Setup(u => u.VendorProfiles).Returns(_vendorProfileRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);

        _handler = new RemoveProductVariantCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _currentUserServiceMock.Object);
    }

    #region Helper Methods

    private RemoveProductVariantCommand CreateValidCommand(string? customSku = null)
    {
        return new RemoveProductVariantCommand(_validProductId, customSku ?? _validSku);
    }

    private void SetupValidContext()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_validUserId.ToString());

        var happyProduct = new ProductBuilder()
            .WithId(_validProductId)
            .WithVariant(_validSku, 10.0m, 5)
            .Build();

        _vendorProfileRepoMock
            .Setup(x => x.GetVendorIdByUserIdAsync(_validUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(happyProduct.VendorId);

        _productRepoMock
            .Setup(x => x.GetProductWithVariantsAsync(_validProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(happyProduct);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_WhenAllDataIsValid_ShouldRemoveVariantAndSave()
    {
        SetupValidContext();
        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDomainThrowsException_ShouldCatchAndReturnValidation()
    {
        SetupValidContext();

        var command = CreateValidCommand(customSku: "UNKNOWN-SKU");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.ErrorMessage.Should().Contain("Variant not found");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
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
            .Setup(x => x.GetProductWithVariantsAsync(_validProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var command = CreateValidCommand();
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotTheProductOwner_ShouldReturnUnauthorized()
    {
        SetupValidContext();

        _vendorProfileRepoMock
            .Setup(x => x.GetVendorIdByUserIdAsync(_validUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var command = CreateValidCommand();
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("permission");
    }

    #endregion
}