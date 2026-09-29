using OmniMart.Domain.Entities;
using System;
using System.Reflection;

namespace OmniMart.Tests.Builders;

public class CategoryAttributeBuilder
{
    private Guid _categoryId = Guid.NewGuid();
    private Guid _productAttributeId = Guid.NewGuid();
    private bool _isRequired = true;
    private ProductAttribute? _productAttribute = null;

    public CategoryAttributeBuilder WithCategory(Guid categoryId) { _categoryId = categoryId; return this; }
    public CategoryAttributeBuilder WithProductAttributeId(Guid id) { _productAttributeId = id; return this; }
    public CategoryAttributeBuilder WithIsRequired(bool isRequired) { _isRequired = isRequired; return this; }

    public CategoryAttributeBuilder WithProductAttributeEntity(ProductAttribute productAttribute)
    {
        _productAttribute = productAttribute;
        return this;
    }

    public CategoryAttribute Build()
    {
        var categoryAttr = new CategoryAttribute(_categoryId, _productAttributeId, _isRequired);

        if (_productAttribute != null)
        {
            typeof(CategoryAttribute).GetProperty("ProductAttribute", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(categoryAttr, _productAttribute);
        }

        return categoryAttr;
    }
}