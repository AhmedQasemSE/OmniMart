using FluentAssertions;
using OmniMart.Domain.Entities;
using System;
using Xunit;

namespace OmniMart.Tests.Domain;

public class ProductAttributeTests
{
    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateSuccessfully()
    {
        var attribute = new ProductAttribute("Color");

        attribute.Id.Should().NotBeEmpty();
        attribute.Name.Should().Be("Color");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WhenNameIsInvalid_ShouldThrowArgumentException(string? invalidName)
    {
        Action act = () => new ProductAttribute(invalidName!);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Name cannot be null or whitespace.*");
    }

    [Fact]
    public void UpdateName_WhenValid_ShouldUpdateName()
    {
        var attribute = new ProductAttribute("OldName");

        attribute.UpdateName("NewName");

        attribute.Name.Should().Be("NewName");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdateName_WhenInvalid_ShouldThrowArgumentException(string? invalidName)
    {
        var attribute = new ProductAttribute("Color");

        Action act = () => attribute.UpdateName(invalidName!);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Name cannot be null or whitespace.*");
    }
}