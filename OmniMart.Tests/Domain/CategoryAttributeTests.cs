using FluentAssertions;
using OmniMart.Domain.Entities;
using System;
using Xunit;

namespace OmniMart.Tests.Domain;

public class CategoryAttributeTests
{
    [Fact]
    public void CategoryAttribute_ShouldBeCreatedSuccessfully()
    {
        var categoryId = Guid.NewGuid();
        var productAttributeId = Guid.NewGuid();

        var categoryAttribute = new CategoryAttribute(categoryId, productAttributeId, true);

        categoryAttribute.Id.Should().NotBeEmpty();
        categoryAttribute.CategoryId.Should().Be(categoryId);
        categoryAttribute.ProductAttributeId.Should().Be(productAttributeId);
        categoryAttribute.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void CategoryAttribute_ShouldThrowArgumentException_WhenCategoryIdIsEmpty()
    {
        var productAttributeId = Guid.NewGuid();

        Action act = () => new CategoryAttribute(Guid.Empty, productAttributeId, true);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*CategoryId cannot be empty.*");
    }

    [Fact]
    public void CategoryAttribute_ShouldThrowArgumentException_WhenProductAttributeIdIsEmpty()
    {
        var categoryId = Guid.NewGuid();

        Action act = () => new CategoryAttribute(categoryId, Guid.Empty, true);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*ProductAttributeId cannot be empty.*");
    }
}