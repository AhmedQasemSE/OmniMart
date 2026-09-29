using FluentAssertions;
using OmniMart.Domain.Entities;
using System;
using System.Linq;
using Xunit;

namespace OmniMart.Tests.Domain;

public class CartTests
{
    private readonly Guid _customerId;
    private readonly Cart _cart;
    

    public CartTests()
    {
        _customerId = Guid.NewGuid();

        _cart = new Cart(_customerId);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WhenCustomerIdIsValid_ShouldCreateCart()
    {
        var cart = new Cart(_customerId);

        cart.CustomerId.Should().Be(_customerId);
        cart.CartItems.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WhenCustomerIdIsEmpty_ShouldThrowArgumentException()
    {
        Action action = () => new Cart(Guid.Empty);

        action.Should().Throw<ArgumentException>()
              .WithMessage("*Customer ID cannot be empty*");
    }

    #endregion

    #region AddItem Tests

    [Fact]
    public void AddItem_WhenCartIsEmpty_ShouldAddNewCartItem()
    {
        var productVariantId = Guid.NewGuid();

        _cart.AddItem(productVariantId, 3);

        _cart.CartItems.Should().HaveCount(1);
        var addedItem = _cart.CartItems.First();
        addedItem.ProductVariantId.Should().Be(productVariantId);
        addedItem.Quantity.Should().Be(3);
    }

    [Fact]
    public void AddItem_WhenItemAlreadyExists_ShouldIncreaseQuantityOnly()
    {
        var productVariantId = Guid.NewGuid();

        _cart.AddItem(productVariantId, 3);
        _cart.AddItem(productVariantId, 3);

        _cart.CartItems.Should().HaveCount(1);
        _cart.CartItems.First().Quantity.Should().Be(6);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_WhenQuantityIsInvalid_ShouldThrowArgumentException(int invalidQuantity)
    {
        Action action = () => _cart.AddItem(Guid.NewGuid(), invalidQuantity);

        action.Should().Throw<ArgumentException>()
              .WithMessage("*greater than zero*");
    }

    [Fact]
    public void AddItem_WhenProductIdIsEmpty_ShouldThrowArgumentException()
    {
        Action action = () => _cart.AddItem(Guid.Empty, 4);

        action.Should().Throw<ArgumentException>()
              .WithMessage("*cannot be empty*");
    }

    #endregion

    #region DecreaseItemQuantity Tests

    [Fact]
    public void DecreaseItemQuantity_WhenAmountIsSmaller_ShouldDecreaseQuantity()
    {
        var productVariantId = Guid.NewGuid();
        _cart.AddItem(productVariantId, 10);

        _cart.DecreaseItemQuantity(productVariantId, 7);

        _cart.CartItems.First().Quantity.Should().Be(3);
    }

    [Fact]
    public void DecreaseItemQuantity_WhenAmountIsBiggerOrEqual_ShouldRemoveItem()
    {
        var productVariantId = Guid.NewGuid();
        _cart.AddItem(productVariantId, 5);

        _cart.DecreaseItemQuantity(productVariantId, 9);

        _cart.CartItems.Should().BeEmpty();
    }

    [Fact]
    public void DecreaseItemQuantity_WhenItemNotFound_ShouldThrowInvalidOperationException()
    {
        Action action = () => _cart.DecreaseItemQuantity(Guid.NewGuid(), 4);

        action.Should().Throw<InvalidOperationException>()
              .WithMessage("*not found in the cart*");
    }

    #endregion

    #region RemoveItem & ClearCart Tests

    [Fact]
    public void RemoveItem_WhenItemExists_ShouldRemoveIt()
    {
        var productVariantId = Guid.NewGuid();
        _cart.AddItem(productVariantId, 10);

        _cart.RemoveItem(productVariantId);

        _cart.CartItems.Should().BeEmpty();
    }

    [Fact]
    public void RemoveItem_WhenItemDoesNotExist_ShouldDoNothing()
    {
        var originalProduct = Guid.NewGuid();
        _cart.AddItem(originalProduct, 5);

        _cart.RemoveItem(Guid.NewGuid()); 

        _cart.CartItems.Should().HaveCount(1);
        _cart.CartItems.First().ProductVariantId.Should().Be(originalProduct);
    }

    [Fact]
    public void ClearCart_WhenCartHasItems_ShouldRemoveAllItems()
    {
        _cart.AddItem(Guid.NewGuid(), 10);
        _cart.AddItem(Guid.NewGuid(), 5);

        _cart.ClearCart();

        _cart.CartItems.Should().BeEmpty();
    }

    #endregion

    #region UpdateItemQuantity Tests

    [Fact]
    public void UpdateItemQuantity_WhenItemExists_ShouldUpdateQuantity()
    {
        var variantId = Guid.NewGuid();
        _cart.AddItem(variantId, 2);

        _cart.UpdateItemQuantity(variantId, 5);

        _cart.CartItems.First().Quantity.Should().Be(5);
    }

    [Fact]
    public void UpdateItemQuantity_WhenItemDoesNotExist_ShouldThrowException()
    {
        Action act = () => _cart.UpdateItemQuantity(Guid.NewGuid(), 5);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*not found in the cart*");
    }

    #endregion
}