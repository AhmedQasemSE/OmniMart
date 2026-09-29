using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Products.Commands;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Command;

public class CreateProductCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ILogger<CreateProductCommandHandler>> _loggerMock;

    private readonly Mock<ICategoryRepository> _categoryRepoMock;
    private readonly Mock<IVendorProfileRepository> _vendorProfileRepoMock;
    private readonly Mock<IProductRepository> _productRepoMock;

    private readonly CreateProductCommandHandler _handler;

    private readonly Guid _validUserId = Guid.NewGuid();
    private readonly Guid _validVendorId = Guid.NewGuid();
    private readonly Guid _validCategoryId = Guid.NewGuid();

    public CreateProductCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _loggerMock = new Mock<ILogger<CreateProductCommandHandler>>();

        _categoryRepoMock = new Mock<ICategoryRepository>();
        _vendorProfileRepoMock = new Mock<IVendorProfileRepository>();
        _productRepoMock = new Mock<IProductRepository>();


        _unitOfWorkMock.Setup(u => u.Categories).Returns(_categoryRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.VendorProfiles).Returns(_vendorProfileRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);

        _handler = new CreateProductCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);
    }

    #region Helper Methods 

    private CreateProductCommand CreateValidCommand()
    {
        return new CreateProductCommand(
            CategoryId: _validCategoryId,
            Name: "Test Product",
            Description: "A great product description",
            BasePrice: 100m,
            Variants: new List<VariantRequestDto>
            {
                new VariantRequestDto("SKU-123", 120m, 50),
                new VariantRequestDto("SKU-456", 130m, 30)
            }
        );
    }

    private void SetupValidContext()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_validUserId.ToString());

        _vendorProfileRepoMock
            .Setup(x => x.GetVendorIdByUserIdAsync(_validUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_validVendorId);

        _categoryRepoMock
            .Setup(x => x.IsCategoryExistsAsync(_validCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _productRepoMock
            .Setup(x => x.GetExistingSkusAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>()); 
    }

    #endregion

    #region 1. Success Path Tests 

    [Fact]
    public async Task Handle_WhenAllDataIsValid_ShouldCreateProductAndReturnSuccessId()
    {
        SetupValidContext();
        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _productRepoMock.Verify(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region 2. Failure Path Tests 

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid-guid-string")]
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
    public async Task Handle_WhenVendorProfileIsNotFound_ShouldReturnNotFound()
    {
        SetupValidContext();

        _vendorProfileRepoMock
            .Setup(x => x.GetVendorIdByUserIdAsync(_validUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("Vendor profile not found");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCategoryDoesNotExist_ShouldReturnNotFound()
    {
        
        SetupValidContext();

        _categoryRepoMock
            .Setup(x => x.IsCategoryExistsAsync(_validCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("Category not found");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSkuAlreadyExists_ShouldReturnConflict()
    {
        SetupValidContext();
        var command = CreateValidCommand();


        _productRepoMock
            .Setup(x => x.GetExistingSkusAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "SKU-123" });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("already exists");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenVariantDataIsInvalidAndThrowsException_ShouldReturnValidationFailure()
    {
        SetupValidContext();

        var command = new CreateProductCommand(
            CategoryId: _validCategoryId,
            Name: "Test Product",
            Description: "Desc",
            BasePrice: 100m,
            Variants: new List<VariantRequestDto>
            {
                new VariantRequestDto("SKU-999", 100m, -5) 
            }
        );

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();

        result.ErrorType.Should().Be(ErrorType.Failure);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}