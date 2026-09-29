using OmniMart.Domain.Common;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OmniMart.Tests.Builders;

public class OrderBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _customerId = Guid.NewGuid();
    private Guid _vendorId = Guid.NewGuid();
    private Guid _paymentGroupId = Guid.NewGuid();
    private PaymentMethod _paymentMethod = PaymentMethod.CreditCard;
    private Address _shippingAddress = new Address("Istanbul", "Istiklal", "34000", "0555555555");

    private OrderStatus? _status = null;
    private DateTimeOffset? _orderDate = null;
    private decimal? _forcedTotalAmount = null;

    private CustomerProfile? _customerEntity = null;
    private VendorProfile? _vendorProfileEntity = null;

    private readonly List<(Guid VariantId, decimal Price, int Quantity, ProductVariant? Variant)> _items = new();

    #region Fluent API Methods

    public OrderBuilder WithId(Guid id) { _id = id; return this; }
    public OrderBuilder WithCustomerId(Guid customerId) { _customerId = customerId; return this; }
    public OrderBuilder WithVendorId(Guid vendorId) { _vendorId = vendorId; return this; }
    public OrderBuilder WithPaymentGroupId(Guid paymentGroupId) { _paymentGroupId = paymentGroupId; return this; }
    public OrderBuilder WithStatus(OrderStatus status) { _status = status; return this; }
    public OrderBuilder WithOrderDate(DateTimeOffset orderDate) { _orderDate = orderDate; return this; }
    public OrderBuilder WithForcedTotalAmount(decimal amount) { _forcedTotalAmount = amount; return this; }

    public OrderBuilder WithItem(Guid variantId, decimal price, int quantity, ProductVariant? variant = null)
    {
        _items.Add((variantId, price, quantity, variant));
        return this;
    }


    public OrderBuilder WithCustomer(CustomerProfile customer)
    {
        _customerEntity = customer;
        _customerId = customer.Id; 
        return this;
    }

    public OrderBuilder WithCustomerEntity(CustomerProfile customer) => WithCustomer(customer);

    public OrderBuilder WithVendorProfile(VendorProfile vendor)
    {
        _vendorProfileEntity = vendor;
        _vendorId = vendor.Id; 
        return this;
    }

    public OrderBuilder WithVendorProfileEntity(VendorProfile vendor) => WithVendorProfile(vendor);

    public OrderBuilder WithAddress(Address address)
    {
        _shippingAddress = address;
        return this;
    }

    #endregion

    public Order Build()
    {
        var order = new Order(_customerId, _vendorId, _paymentGroupId, _paymentMethod, _shippingAddress);

        SetPrivateProperty(order, "Id", _id);

        if (_status.HasValue) SetPrivateProperty(order, "Status", _status.Value);
        if (_orderDate.HasValue) SetPrivateProperty(order, "OrderDate", _orderDate.Value);

        foreach (var item in _items)
        {
            order.AddOrderItem(item.VariantId, item.Price, item.Quantity);

            if (item.Variant != null)
            {
                var addedOrderItem = order.OrderItems.Last();
                SetPrivateProperty(addedOrderItem, "ProductVariant", item.Variant);
            }
        }

        if (_forcedTotalAmount.HasValue) SetPrivateProperty(order, "TotalAmount", _forcedTotalAmount.Value);
        if (_customerEntity != null) SetPrivateProperty(order, "Customer", _customerEntity);
        if (_vendorProfileEntity != null) SetPrivateProperty(order, "VendorProfile", _vendorProfileEntity);

        return order;
    }

    private void SetPrivateProperty(object instance, string propertyName, object value)
    {
        var type = instance.GetType();
        PropertyInfo? property = null;

        while (type != null && property == null)
        {
            property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            type = type.BaseType;
        }

        property?.SetValue(instance, value);
    }
}