using OmniMart.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OmniMart.Tests.Builders;

public class PaymentGroupBuilder
{
    private Guid? _id = null;
    private Guid _customerId = Guid.NewGuid();
    private DateTimeOffset? _createdAt = null;
    private bool _isPaid = false;
    private string _stripeSessionId = string.Empty;

    private readonly List<Order> _orders = new();

    public PaymentGroupBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public PaymentGroupBuilder WithCustomerId(Guid customerId)
    {
        _customerId = customerId;
        return this;
    }

    public PaymentGroupBuilder WithCreatedAt(DateTimeOffset createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public PaymentGroupBuilder WithOrder(Order order)
    {
        _orders.Add(order);
        return this;
    }

    public PaymentGroupBuilder AsPaid(string stripeSessionId = "test_session_123")
    {
        _isPaid = true;
        _stripeSessionId = stripeSessionId;
        return this;
    }

    public PaymentGroup Build()
    {
        var paymentGroup = new PaymentGroup(_customerId);

        if (_id.HasValue)
        {
            SetPrivateProperty(paymentGroup, "Id", _id.Value);
        }

        if (_createdAt.HasValue)
        {
            SetPrivateProperty(paymentGroup, "CreatedAt", _createdAt.Value);
        }

        foreach (var order in _orders)
        {
            paymentGroup.AddOrder(order);
        }

        if (_isPaid)
        {
            paymentGroup.MarkAsPaid(_stripeSessionId);
        }

        return paymentGroup;
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