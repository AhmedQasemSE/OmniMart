using OmniMart.Domain.Entities;
using System;
using System.Reflection;

namespace OmniMart.Tests.Builders;

public class ProductAttributeBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "Color";

    public ProductAttributeBuilder WithId(Guid id) { _id = id; return this; }
    public ProductAttributeBuilder WithName(string name) { _name = name; return this; }

    public ProductAttribute Build()
    {
        var productAttr = new ProductAttribute(_name);

        SetPrivateProperty(productAttr, "Id", _id);

        return productAttr;
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