using OmniMart.Domain.Entities;
using System;
using System.Reflection;

namespace OmniMart.Tests.Builders;

public class CartItemBuilder
{
    private Guid _cartId = Guid.NewGuid();
    private Guid _productVariantId = Guid.NewGuid();
    private int _quantity = 1;
    private ProductVariant? _productVariant = null;

    public CartItemBuilder WithCartId(Guid cartId) { _cartId = cartId; return this; }
    public CartItemBuilder WithProductVariantId(Guid variantId) { _productVariantId = variantId; return this; }
    public CartItemBuilder WithQuantity(int quantity) { _quantity = quantity; return this; }

    public CartItemBuilder WithProductVariantEntity(ProductVariant variant)
    {
        _productVariant = variant;
        _productVariantId = variant.Id; 
        return this;
    }

    public CartItem Build()
    {
        var item = new CartItem(_cartId, _productVariantId, _quantity);

        if (_productVariant != null)
        {
            typeof(CartItem).GetProperty("ProductVariant", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(item, _productVariant);
        }

        return item;
    }
}