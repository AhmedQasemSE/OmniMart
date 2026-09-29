using FluentAssertions;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Events;
using System;
using System.Linq;
using Xunit;

namespace OmniMart.Tests.Domain;

public class ProductVariantTests
{
    private readonly Guid _validProductId;
    private readonly ProductVariant _variant; 

    public ProductVariantTests()
    {
        _validProductId = Guid.NewGuid();

        _variant = new ProductVariant(_validProductId, "SKU-123", 100m, 50);
    }

    #region 1. Constructor Tests 

    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateVariantSuccessfully()
    {
        var variant = new ProductVariant(_validProductId, "SKU-123", 100m, 50);

        variant.Id.Should().NotBeEmpty();
        variant.ProductId.Should().Be(_validProductId);
        variant.SKU.Should().Be("SKU-123");
        variant.Price.Should().Be(100m);
        variant.StockQuantity.Should().Be(50);
        variant.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WhenProductIdIsEmpty_ShouldThrowArgumentException()
    {
        Action act = () => new ProductVariant(Guid.Empty, "SKU-123", 100m, 50);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Product ID cannot be empty.*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WhenSkuIsInvalid_ShouldThrowArgumentException(string? invalidSku)
    {
        Action act = () => new ProductVariant(_validProductId, invalidSku!, 100m, 50);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*SKU cannot be null or empty.*");
    }

    [Fact]
    public void Constructor_WhenPriceIsNegative_ShouldThrowArgumentException()
    {
        Action act = () => new ProductVariant(_validProductId, "SKU-123", -10m, 50);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Price cannot be negative.*");
    }

    [Fact]
    public void Constructor_WhenStockQuantityIsNegative_ShouldThrowArgumentException()
    {
        Action act = () => new ProductVariant(_validProductId, "SKU-123", 100m, -5);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Stock quantity cannot be negative.*");
    }

    #endregion

    #region 2. UpdateDetails Tests 

    [Fact]
    public void UpdateDetails_WhenDataIsValid_ShouldUpdatePriceAndStock()
    {
        _variant.UpdateDetails(150m, 70);

        _variant.Price.Should().Be(150m);
        _variant.StockQuantity.Should().Be(70);
    }

    [Fact]
    public void UpdateDetails_WhenPriceIsNegative_ShouldThrowArgumentException()
    {
        Action act = () => _variant.UpdateDetails(-5m, 70);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Price cannot be negative.*");
    }

    [Fact]
    public void UpdateDetails_WhenStockIsNegative_ShouldThrowArgumentException()
    {
        Action act = () => _variant.UpdateDetails(150m, -1);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Stock cannot be negative.*");
    }
    [Fact]
    public void UpdateDetails_WhenCategoryIdIsEmpty_ShouldThrowArgumentException()
    {
        Action act = () => _variant.UpdateDetails(150m, 70);
        act.Should().NotThrow(); 
    }
    #endregion

    #region 3. Delete & Restore Tests 

    [Fact]
    public void MarkAsDeleted_ShouldSetIsDeletedToTrue()
    {
        _variant.MarkAsDeleted();

        _variant.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Restore_Parameterless_ShouldSetIsDeletedToFalse()
    {
        _variant.MarkAsDeleted(); 

        _variant.Restore(); 

        _variant.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Restore_WithParameters_WhenValid_ShouldRestoreAndUpdateData()
    {
        _variant.MarkAsDeleted();

        _variant.Restore(120m, 30);

        _variant.IsDeleted.Should().BeFalse();
        _variant.Price.Should().Be(120m);
        _variant.StockQuantity.Should().Be(30);
    }

    [Fact]
    public void Restore_WithParameters_WhenPriceIsNegative_ShouldThrowArgumentException()
    {
        Action act = () => _variant.Restore(-10m, 30);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Price cannot be negative.*");
    }

    [Fact]
    public void Restore_WithParameters_WhenStockIsNegative_ShouldThrowArgumentException()
    {
        Action act = () => _variant.Restore(120m, -5);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Stock quantity cannot be negative.*");
    }

    #endregion

    #region 4. Attributes Management Tests 

    [Fact]
    public void AddAttributeValue_WhenDataIsValid_ShouldAddAttributeToCollection()
    {
        var attributeId = Guid.NewGuid();
        var valueId = "color-red-id";

        _variant.AddAttributeValue(attributeId, valueId);

        _variant.VariantAttributeValues.Should().ContainSingle();
        var addedAttribute = _variant.VariantAttributeValues.First();
        addedAttribute.ProductAttributeId.Should().Be(attributeId);
        addedAttribute.Value.Should().Be(valueId);
    }

    [Fact]
    public void AddAttributeValue_WhenAttributeAlreadyExists_ShouldThrowInvalidOperationException()
    {
        var attributeId = Guid.NewGuid();

        _variant.AddAttributeValue(attributeId, "color-red-id");

        Action act = () => _variant.AddAttributeValue(attributeId, "color-blue-id");

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("This attribute has already been added to the variant.");
    }

    [Fact]
    public void RemoveAttributeValue_WhenAttributeExists_ShouldRemoveFromCollection()
    {
        var attributeId = Guid.NewGuid();
        _variant.AddAttributeValue(attributeId, "color-red-id");

        _variant.RemoveAttributeValue(attributeId);

        _variant.VariantAttributeValues.Should().BeEmpty();
    }

    [Fact]
    public void RemoveAttributeValue_WhenAttributeDoesNotExist_ShouldDoNothing()
    {
        var attributeId = Guid.NewGuid();
        var randomAttributeId = Guid.NewGuid();

        _variant.AddAttributeValue(attributeId, "color-red-id");

        Action act = () => _variant.RemoveAttributeValue(randomAttributeId);

        act.Should().NotThrow();
        _variant.VariantAttributeValues.Should().ContainSingle(); 
    }

    #endregion

    #region 5. Basic Stock Operations Tests 

    [Fact]
    public void IncreaseStock_WhenQuantityIsValid_ShouldIncreaseStockQuantity()
    {
        _variant.IncreaseStock(20);

        _variant.StockQuantity.Should().Be(70);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void IncreaseStock_WhenQuantityIsInvalid_ShouldThrowArgumentException(int invalidQuantity)
    {
        Action act = () => _variant.IncreaseStock(invalidQuantity);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Quantity must be greater than zero.*");
    }

    [Fact]
    public void DecreaseStock_WhenQuantityIsValid_ShouldDecreaseStockQuantity()
    {
        _variant.DecreaseStock(20);

        _variant.StockQuantity.Should().Be(30);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void DecreaseStock_WhenQuantityIsInvalid_ShouldThrowArgumentException(int invalidQuantity)
    {
        Action act = () => _variant.DecreaseStock(invalidQuantity);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Quantity must be greater than zero.*");
    }

    [Fact]
    public void DecreaseStock_WhenQuantityIsGreaterThanAvailableStock_ShouldThrowInvalidOperationException()
    {
        Action act = () => _variant.DecreaseStock(60);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Insufficient stock to decrease.*");
    }

    [Fact]
    public void DecreaseStock_WhenStockReachesZero_ShouldAddOutOfStockDomainEvent()
    {
        _variant.DecreaseStock(50);

        _variant.StockQuantity.Should().Be(0);

        _variant.DomainEvents.Should().ContainSingle(e => e is ProductOutOfStockEvent);
    }

    #endregion

    #region 6. Advanced Stock Management Tests


    [Fact]
    public void ReserveStock_WhenQuantityIsValid_ShouldIncreaseReservedAndDecreaseAvailableStock()
    {
        _variant.ReserveStock(20);

        _variant.ReservedStock.Should().Be(20);
        _variant.AvailableStock.Should().Be(30);
        _variant.StockQuantity.Should().Be(50);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ReserveStock_WhenQuantityIsInvalid_ShouldThrowArgumentException(int invalidQuantity)
    {
        Action act = () => _variant.ReserveStock(invalidQuantity);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Quantity must be greater than zero.*");
    }

    [Fact]
    public void ReserveStock_WhenQuantityExceedsAvailableStock_ShouldThrowInvalidOperationException()
    {
        _variant.ReserveStock(40); 

        Action act = () => _variant.ReserveStock(20);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Insufficient available stock to reserve.*");
    }


    [Fact]
    public void CommitReservedStock_WhenValid_ShouldDecreaseTotalStockAndClearReserved()
    {
        _variant.ReserveStock(20);

        _variant.CommitReservedStock(20);

        _variant.ReservedStock.Should().Be(0);
        _variant.StockQuantity.Should().Be(30);
        _variant.AvailableStock.Should().Be(30);
    }

    [Fact]
    public void CommitReservedStock_WhenQuantityExceedsReserved_ShouldThrowInvalidOperationException()
    {
        _variant.ReserveStock(10);

        Action act = () => _variant.CommitReservedStock(15);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Cannot commit more than reserved.*");
    }

    [Fact]
    public void CommitReservedStock_WhenStockReachesZero_ShouldAddOutOfStockDomainEvent()
    {
        _variant.ReserveStock(50);

        _variant.CommitReservedStock(50);

        _variant.StockQuantity.Should().Be(0);
        _variant.DomainEvents.Should().ContainSingle(e => e is ProductOutOfStockEvent);
    }


    [Fact]
    public void ReleaseReservedStock_WhenValid_ShouldDecreaseReservedAndRestoreAvailableStock()
    {
        _variant.ReserveStock(20);

        _variant.ReleaseReservedStock(20);

        _variant.ReservedStock.Should().Be(0);
        _variant.AvailableStock.Should().Be(50);
        _variant.StockQuantity.Should().Be(50);
    }

     

    #endregion
}
