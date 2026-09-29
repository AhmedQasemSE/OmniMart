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
using System.IO;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Command;

public class UploadProductImageCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ILogger<UploadProductImageCommandHandler>> _loggerMock;

    private readonly Mock<IVendorProfileRepository> _vendorProfileRepoMock;
    private readonly Mock<IProductRepository> _productRepoMock;

    private readonly UploadProductImageCommandHandler _handler;

    private readonly Guid _validUserId = Guid.NewGuid();
    private readonly Guid _validProductId = Guid.NewGuid();
    private readonly ImageStorageResult _validUploadResult = new("https://cloudinary.com/img1.jpg", "public_id_123");

    public UploadProductImageCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _loggerMock = new Mock<ILogger<UploadProductImageCommandHandler>>();

        _vendorProfileRepoMock = new Mock<IVendorProfileRepository>();
        _productRepoMock = new Mock<IProductRepository>();

        _unitOfWorkMock.Setup(u => u.VendorProfiles).Returns(_vendorProfileRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);

        _handler = new UploadProductImageCommandHandler(
            _unitOfWorkMock.Object,
            _fileStorageServiceMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);
    }

    #region Helper Methods

    private UploadProductImageCommand CreateValidCommand()
    {
        return new UploadProductImageCommand(
            ProductId: _validProductId,
            FileStream: new MemoryStream(new byte[] { 1, 2, 3 }), 
            FileName: "test-image.jpg",
            IsPrimary: true
        );
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
            .Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<Product, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Product, object>>[]>())) 
            .ReturnsAsync(happyProduct);

        _fileStorageServiceMock
            .Setup(x => x.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(_validUploadResult); 
    }

    #endregion
    #region Tests
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
        _fileStorageServiceMock.Verify(x => x.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>()), Times.Never);
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
    public async Task Handle_WhenCloudinaryUploadFails_ShouldReturnFailureWithoutSaving()
    {
        SetupValidContext();

        _fileStorageServiceMock
            .Setup(x => x.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Cloudinary servers are down!"));

        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();

        result.ErrorType.Should().Be(ErrorType.Failure);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAllDataIsValid_ShouldUploadImageAndSaveToDatabase()
    {
        SetupValidContext(); 
        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _fileStorageServiceMock.Verify(x => x.UploadImageAsync(It.IsAny<Stream>(), command.FileName), Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _fileStorageServiceMock.Verify(x => x.DeleteImageAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotTheProductOwner_ShouldReturnUnauthorized()
    {
        SetupValidContext();

        var productOwnedByAnotherVendor = new ProductBuilder()
            .Build();

        _productRepoMock
            .Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<Product, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Product, object>>[]>()))
            .ReturnsAsync(productOwnedByAnotherVendor);

        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);

        _fileStorageServiceMock.Verify(x => x.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>()), Times.Never);
    }
    [Fact]
    public async Task Handle_WhenDatabaseFails_ShouldRollbackCloudinaryImageAndReturnFailure()
    {
        SetupValidContext(); 

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection lost!"));

        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation); 
        

        _fileStorageServiceMock.Verify(x => x.UploadImageAsync(It.IsAny<Stream>(), command.FileName), Times.Once);

        _fileStorageServiceMock.Verify(x => x.DeleteImageAsync(_validUploadResult.PublicId), Times.Once);
    }
    #endregion
}