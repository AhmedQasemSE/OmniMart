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
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Command;

public class AssignValuesToVariantCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<ILogger<AssignValuesToVariantCommandHandler>> _loggerMock;

    private readonly Mock<IVendorProfileRepository> _vendorProfileRepoMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly Mock<ICategoryRepository> _categoryRepoMock;

    private readonly AssignValuesToVariantCommandHandler _handler;

    private readonly Guid _validUserId = Guid.NewGuid();
    private readonly Guid _validProductId = Guid.NewGuid();
    private readonly Guid _validCategoryId = Guid.NewGuid();
    private readonly string _validSku = "SKU-TEST-123";
    private readonly byte[] _validRowVersion = new byte[] { 1, 2, 3, 4 };

    private readonly Guid _requiredAttrId = Guid.NewGuid();
    private readonly Guid _optionalAttrId = Guid.NewGuid();

    public AssignValuesToVariantCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _cacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<AssignValuesToVariantCommandHandler>>();

        _vendorProfileRepoMock = new Mock<IVendorProfileRepository>();
        _productRepoMock = new Mock<IProductRepository>();
        _categoryRepoMock = new Mock<ICategoryRepository>();

        _unitOfWorkMock.Setup(u => u.VendorProfiles).Returns(_vendorProfileRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Categories).Returns(_categoryRepoMock.Object);

        _handler = new AssignValuesToVariantCommandHandler(
            _unitOfWorkMock.Object,
            _currentUserServiceMock.Object,
            _cacheMock.Object,
            _loggerMock.Object);
    }

    #region Helper Methods

    private AssignValuesToVariantCommand CreateValidCommand(List<VariantAttributeValueDto>? values = null)
    {
        var defaultValues = new List<VariantAttributeValueDto>
        {
            new VariantAttributeValueDto(_requiredAttrId, "Default Value")
        };

        return new AssignValuesToVariantCommand(
            _validProductId,
            _validSku,
            values ?? defaultValues,
            _validRowVersion);
    }

    private void SetupValidContext()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_validUserId.ToString());

        var happyProduct = new ProductBuilder()
            .WithId(_validProductId)
            .WithCategoryId(_validCategoryId)
            .Build();
        happyProduct.AddVariant(_validSku, 100m, 10);

        var variant = happyProduct.Variants.First();
        variant.AddAttributeValue(_requiredAttrId, "Old Valid Value");

        _vendorProfileRepoMock
            .Setup(x => x.GetVendorIdByUserIdAsync(_validUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(happyProduct.VendorId);

        _productRepoMock
            .Setup(x => x.GetProductWithAllVariantsForUpdateAsync(_validProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(happyProduct);

        var category = new CategoryBuilder()
            .WithId(_validCategoryId)
            .Build();

        category.AddAttribute(_requiredAttrId, isRequired: true);
        category.AddAttribute(_optionalAttrId, isRequired: false);

        _categoryRepoMock
            .Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<Category, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Category, object>>[]>()))
            .ReturnsAsync(category);
    }

    #endregion

    #region Tests - Part 1: Gates & Security 

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
            .Setup(x => x.GetProductWithAllVariantsForUpdateAsync(_validProductId, It.IsAny<CancellationToken>()))
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

    [Fact]
    public async Task Handle_WhenVariantNotFound_ShouldReturnNotFound()
    {
        SetupValidContext();
        var command = new AssignValuesToVariantCommand(_validProductId, "WRONG-SKU", new List<VariantAttributeValueDto>(), _validRowVersion);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("Active variant not found");
    }

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ShouldReturnFailure()
    {
        SetupValidContext();

        _categoryRepoMock
            .Setup(x => x.GetAsync(
                It.IsAny<Expression<Func<Category, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Expression<Func<Category, object>>[]>()))
            .ReturnsAsync((Category?)null);

        var command = CreateValidCommand();
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Failure);
        result.ErrorMessage.Should().Contain("category not found");
    }

    #endregion

    #region Tests - Part 2: Business Logic Validation 

    [Fact]
    public async Task Handle_WhenRequiredAttributeIsOmitted_ShouldReturnValidation()
    {
        SetupValidContext();

        var invalidValues = new List<VariantAttributeValueDto>
        {
            new VariantAttributeValueDto(_optionalAttrId, "Some Value")
        };
        var command = CreateValidCommand(invalidValues);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.ErrorMessage.Should().Contain("must provide values for all required attributes");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAddingAttributeNotBelongingToCategory_ShouldReturnValidation()
    {
        SetupValidContext();

        var alienAttributeId = Guid.NewGuid();
        var invalidValues = new List<VariantAttributeValueDto>
        {
            new VariantAttributeValueDto(_requiredAttrId, "Valid Value"),
            new VariantAttributeValueDto(alienAttributeId, "Alien Value") 
        };
        var command = CreateValidCommand(invalidValues);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.ErrorMessage.Should().Contain("do not belong to this product's category");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Tests - Part 3: Happy Path, Concurrency & Cache 

    [Fact]
    public async Task Handle_WhenAllDataIsValid_ShouldAssignValuesSaveAndClearCache()
    {
        SetupValidContext();

        var newValues = new List<VariantAttributeValueDto>
        {
            new VariantAttributeValueDto(_requiredAttrId, "Updated Required Value"), 
            new VariantAttributeValueDto(_optionalAttrId, "New Optional Value")      
        };
        var command = CreateValidCommand(newValues);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _unitOfWorkMock.Verify(x => x.Products.SetOriginalRowVersion(It.IsAny<Product>(), _validRowVersion), Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _cacheMock.Verify(x => x.RemoveAsync($"Product_Details_{_validProductId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDbUpdateConcurrencyExceptionThrown_ShouldReturnConflict()
    {
        SetupValidContext();
        var command = CreateValidCommand();

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("modified by another user");
    }

    [Fact]
    public async Task Handle_WhenDomainThrowsInvalidOperationException_ShouldCatchAndReturnValidation()
    {
        SetupValidContext();
        var command = CreateValidCommand();

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Custom domain rule violated"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.ErrorMessage.Should().Contain("Custom domain rule violated"); 
    }

    #endregion

}