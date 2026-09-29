using FluentAssertions;
using OmniMart.Domain.Entities;
using System;
using System.Linq;
using Xunit;

namespace OmniMart.Tests.Domain;

public class CategoryTests
{
    private Category CreateValidCategory(
        string name = "Electronics",
        Guid? parentId = null,
        string description = "Electronic devices and gadgets",
        bool isActive = true)
    {
        return new Category(name, parentId, description, isActive);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldCreateCategory_WhenDataIsValid()
    {
        var category = CreateValidCategory();

        category.Name.Should().Be("Electronics");
        category.Description.Should().Be("Electronic devices and gadgets");
        category.ParentCategoryId.Should().BeNull();
        category.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldCreateCategory_WithParentIdAndInactive()
    {
        var parentId = Guid.NewGuid();
        var category = CreateValidCategory(parentId: parentId, isActive: false);

        category.ParentCategoryId.Should().Be(parentId);
        category.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenNameIsNullOrEmpty()
    {
        Action act = () => CreateValidCategory(name: "");
        act.Should().Throw<ArgumentException>().WithMessage("*Name cannot be null or empty*");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenDescriptionIsNullOrEmpty()
    {
        Action act = () => CreateValidCategory(description: "");
        act.Should().Throw<ArgumentException>().WithMessage("*Description cannot be null or empty*");
    }

    #endregion

    #region UpdateDetails Tests

    [Fact]
    public void UpdateDetails_ShouldUpdateCategoryDetailsSuccessfully()
    {
        var category = CreateValidCategory();
        var newParentId = Guid.NewGuid();

        category.UpdateDetails("Updated Electronics", "Updated description", newParentId);

        category.Name.Should().Be("Updated Electronics");
        category.Description.Should().Be("Updated description");
        category.ParentCategoryId.Should().Be(newParentId);
    }

    [Theory]
    [InlineData("", "Valid description")]
    [InlineData("Valid Name", "")]
    public void UpdateDetails_ShouldThrowArgumentException_WhenNameOrDescriptionIsInvalid(string invalidName, string invalidDescription)
    {
        var category = CreateValidCategory();

        Action act = () => category.UpdateDetails(invalidName, invalidDescription, null);

        act.Should().Throw<ArgumentException>();
    }

    #endregion
    #region State Management Tests (Active/Delete/Restore)

    [Fact]
    public void ToggleActiveStatus_ShouldToggleIsActiveProperty()
    {
        var category = CreateValidCategory(); 

        category.ToggleActiveStatus();
        category.IsActive.Should().BeFalse();

        category.ToggleActiveStatus();
        category.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ToggleActiveStatus_ShouldNotChangeIsActive_WhenCategoryIsDeleted()
    {
        var category = CreateValidCategory();
        category.SoftDelete();

        category.ToggleActiveStatus();

        category.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        var category = CreateValidCategory();

        category.Deactivate();

        category.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrueAndIsDeletedToFalse()
    {
        var category = CreateValidCategory();
        category.SoftDelete(); 

        category.Activate();

        category.IsActive.Should().BeTrue();
        category.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void SoftDelete_ShouldSetIsDeletedToTrueAndIsActiveToFalse()
    {
        var category = CreateValidCategory();

        category.SoftDelete();

        category.IsDeleted.Should().BeTrue();
        category.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Restore_ShouldSetIsDeletedToFalseAndIsActiveToFalse()
    {
        var category = CreateValidCategory();
        category.SoftDelete();

        category.Restore();

        category.IsDeleted.Should().BeFalse();
        category.IsActive.Should().BeFalse();
    }

    #endregion

    #region Category Attributes Tests

    [Fact]
    public void AddAttribute_ShouldAddAttribute_WhenItDoesNotExist()
    {
        var category = CreateValidCategory();
        var attributeId = Guid.NewGuid();

        category.AddAttribute(attributeId, true);

        category.CategoryAttributes.Should().HaveCount(1);
        var addedAttribute = category.CategoryAttributes.First();
        addedAttribute.ProductAttributeId.Should().Be(attributeId);
        addedAttribute.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void AddAttribute_ShouldThrowInvalidOperationException_WhenAttributeAlreadyExists()
    {
        var category = CreateValidCategory();
        var attributeId = Guid.NewGuid();
        category.AddAttribute(attributeId, true);

        Action act = () => category.AddAttribute(attributeId, false);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RemoveAttribute_ShouldRemoveAttribute_WhenItExists()
    {
        var category = CreateValidCategory();
        var attributeId = Guid.NewGuid();
        category.AddAttribute(attributeId, true);

        category.RemoveAttribute(attributeId);

        category.CategoryAttributes.Should().BeEmpty();
    }

    [Fact]
    public void RemoveAttribute_ShouldThrowInvalidOperationException_WhenAttributeDoesNotExist()
    {
        var category = CreateValidCategory();
        var nonExistentAttributeId = Guid.NewGuid();

        Action act = () => category.RemoveAttribute(nonExistentAttributeId);

        act.Should().Throw<InvalidOperationException>();
    }

    #endregion
}
