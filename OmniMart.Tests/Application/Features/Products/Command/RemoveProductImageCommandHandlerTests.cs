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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Command;

public class RemoveProductImageCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ILogger<RemoveProductImageCommandHandler>> _loggerMock;

    private readonly Mock<IVendorProfileRepository> _vendorProfileRepoMock;
    private readonly Mock<IProductRepository> _productRepoMock;

    private readonly RemoveProductImageCommandHandler _handler;

    private readonly Guid _validUserId = Guid.NewGuid();
    private readonly Guid _validProductId = Guid.NewGuid();
    private readonly Guid _validImageId = Guid.NewGuid();

    public RemoveProductImageCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _loggerMock = new Mock<ILogger<RemoveProductImageCommandHandler>>();

        _vendorProfileRepoMock = new Mock<IVendorProfileRepository>();
        _productRepoMock = new Mock<IProductRepository>();

        _unitOfWorkMock.Setup(u => u.VendorProfiles).Returns(_vendorProfileRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);

        _handler = new RemoveProductImageCommandHandler(
            _unitOfWorkMock.Object,
            _fileStorageServiceMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);
    }

    #region Helper Methods

    private RemoveProductImageCommand CreateValidCommand(Guid? customImageId = null)
    {
        return new RemoveProductImageCommand(_validProductId, customImageId ?? _validImageId);
    }

    private void SetupValidContext()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_validUserId.ToString());

        var happyProduct = new ProductBuilder()
              .WithId(_validProductId)
              .WithVendorIds(Guid.NewGuid(), _validUserId)
              .WithImage(_validImageId, publicId: "test_cloudinary_id")
              .WithPrimaryImage(_validImageId, publicId: "test_cloudinary_id") 
              .Build();

        _vendorProfileRepoMock
            .Setup(x => x.GetVendorIdByUserIdAsync(_validUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(happyProduct.VendorId);

        _productRepoMock
            .Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<Product, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Product, object>>[]>()))
            .ReturnsAsync(happyProduct);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_WhenAllDataIsValid_ShouldRemoveImageSaveAndCallCloudinary()
    {
        SetupValidContext();
        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _fileStorageServiceMock.Verify(x => x.DeleteImageAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenImageDoesNotExistInProduct_ShouldReturnNotFound()
    {
        SetupValidContext();
        var command = CreateValidCommand(customImageId: Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _fileStorageServiceMock.Verify(x => x.DeleteImageAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenExceptionIsThrown_ShouldCatchAndReturnFailure()
    {
        SetupValidContext();
        var command = CreateValidCommand();

        _fileStorageServiceMock
            .Setup(x => x.DeleteImageAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Cloudinary timeout error"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Failure);
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
            .Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<Product, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Product, object>>[]>()))
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
    }

    #endregion
}