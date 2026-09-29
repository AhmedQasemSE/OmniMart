using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
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

public class UpdateProductCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<UpdateProductCommandHandler>> _loggerMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDistributedCache> _cacheMock;

    private readonly Mock<IVendorProfileRepository> _vendorProfileRepoMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly Mock<ICategoryRepository> _categoryRepoMock;

    private readonly UpdateProductCommandHandler _handler;

    private readonly Guid _validUserId = Guid.NewGuid();
    private readonly Guid _validProductId = Guid.NewGuid();
    private readonly Guid _oldCategoryId = Guid.NewGuid();
    private readonly Guid _newCategoryId = Guid.NewGuid();
    private readonly byte[] _validRowVersion = new byte[] { 1, 2, 3, 4 };

    public UpdateProductCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<UpdateProductCommandHandler>>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _cacheMock = new Mock<IDistributedCache>();

        _vendorProfileRepoMock = new Mock<IVendorProfileRepository>();
        _productRepoMock = new Mock<IProductRepository>();
        _categoryRepoMock = new Mock<ICategoryRepository>();

        _unitOfWorkMock.Setup(u => u.VendorProfiles).Returns(_vendorProfileRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Categories).Returns(_categoryRepoMock.Object);

        _handler = new UpdateProductCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _currentUserServiceMock.Object,
            _cacheMock.Object);
    }

    #region Helper Methods

    private UpdateProductCommand CreateValidCommand(Guid? categoryId = null, string name = "Updated Name")
    {
        return new UpdateProductCommand(
            ProductId: _validProductId,
            Name: name,
            Description: "Updated Description",
            BasePrice: 150m,
            CategoryId: categoryId ?? _oldCategoryId, 
            RowVersion: _validRowVersion
        );
    }

    private void SetupValidContext()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_validUserId.ToString());

        var happyProduct = new ProductBuilder()
            .WithId(_validProductId)
            .WithCategory(_oldCategoryId)
            .Build();

        _vendorProfileRepoMock
            .Setup(x => x.GetVendorIdByUserIdAsync(_validUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(happyProduct.VendorId);

        _productRepoMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(happyProduct);

        _categoryRepoMock
            .Setup(x => x.IsCategoryExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_WhenAllDataIsValid_ShouldUpdateProductAndClearCache()
    {
        SetupValidContext();
        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(_validProductId);

        _productRepoMock.Verify(x => x.SetOriginalRowVersion(It.IsAny<Product>(), _validRowVersion), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(x => x.RemoveAsync($"Product_Details_{_validProductId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
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
    public async Task Handle_WhenProductIsNotFound_ShouldReturnNotFound()
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
    public async Task Handle_WhenUserIsNotTheProductOwner_ShouldReturnUnauthorized()
    {
        SetupValidContext();
       
        var productOwnedByAnotherVendor = new ProductBuilder()
            .Build();

        _productRepoMock
                .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(productOwnedByAnotherVendor);
       
        var command = CreateValidCommand();
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("do not have permission");
    }

    [Fact]
    public async Task Handle_WhenCategoryIsChangedAndNewCategoryDoesNotExist_ShouldReturnNotFound()
    {
        SetupValidContext();
        _categoryRepoMock
            .Setup(x => x.IsCategoryExistsAsync(_newCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = CreateValidCommand(categoryId: _newCategoryId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenConcurrencyConflictOccurs_ShouldReturnConflict()
    {
        SetupValidContext();
        var command = CreateValidCommand();

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("modified by someone else");
    }

    [Fact]
    public async Task Handle_WhenDomainValidationFails_ShouldReturnFailure()
    {
        SetupValidContext();

        var command = CreateValidCommand(name: "   ");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Failure);
    }

    #endregion
}