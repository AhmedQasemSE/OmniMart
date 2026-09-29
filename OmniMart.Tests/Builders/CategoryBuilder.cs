using OmniMart.Domain.Entities;
using System;
using System.Reflection;

namespace OmniMart.Tests.Builders;

public class CategoryBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _name = "Electronics";
    private string _description = "Electronic devices and gadgets";
    private Guid? _parentCategoryId = null;
    private bool _isActive = true;
    private Category? _parentCategoryEntity = null;

    public CategoryBuilder WithId(Guid id) { _id = id; return this; }
    public CategoryBuilder WithName(string name) { _name = name; return this; }
    public CategoryBuilder WithParent(Guid? parentId) { _parentCategoryId = parentId; return this; }
    public CategoryBuilder WithIsActive(bool isActive) { _isActive = isActive; return this; }

    public CategoryBuilder WithParentCategoryEntity(Category parent)
    {
        _parentCategoryEntity = parent;
        return this;
    }

    public Category Build()
    {
        var category = new Category(_name, _parentCategoryId, _description, _isActive);

        var idProperty = typeof(Category).GetProperty("Id", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        idProperty?.SetValue(category, _id);

        if (_parentCategoryEntity != null)
        {
            var parentProperty = typeof(Category).GetProperty("ParentCategory", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            parentProperty?.SetValue(category, _parentCategoryEntity);
        }

        return category;
    }
}