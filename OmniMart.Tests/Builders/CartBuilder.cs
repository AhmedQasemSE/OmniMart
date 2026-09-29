using OmniMart.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OmniMart.Tests.Builders;

public class CartBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _customerId = Guid.NewGuid();
    private readonly List<CartItem> _cartItems = new();

    public CartBuilder WithId(Guid id) { _id = id; return this; }
    public CartBuilder WithCustomerId(Guid customerId) { _customerId = customerId; return this; }

    public CartBuilder WithCartItem(CartItem item)
    {
        _cartItems.Add(item);
        return this;
    }

    public Cart Build()
    {
        var cart = (Cart)Activator.CreateInstance(typeof(Cart), nonPublic: true)!;

        typeof(Cart).GetProperty("Id", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(cart, _id);
        typeof(Cart).GetProperty("CustomerId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(cart, _customerId);

        var itemsField = typeof(Cart).GetField("_cartItems", BindingFlags.NonPublic | BindingFlags.Instance);
        itemsField?.SetValue(cart, _cartItems);

        return cart;
    }
}