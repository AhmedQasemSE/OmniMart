using FluentAssertions;
using OmniMart.Domain.Entities;
using System;
using Xunit;

namespace OmniMart.Tests.Domain;

public class VariantAttributeValueTests
{
    private readonly Guid _validVariantId = Guid.NewGuid();
    private readonly Guid _validAttributeId = Guid.NewGuid();
    private readonly string _validValue = "Color-Red";

    #region 1. Constructor Tests

    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateSuccessfully()
    {
        var attributeValue = new VariantAttributeValue(_validVariantId, _validAttributeId, _validValue);

        attributeValue.Id.Should().NotBeEmpty();
        attributeValue.ProductVariantId.Should().Be(_validVariantId);
        attributeValue.ProductAttributeId.Should().Be(_validAttributeId);
        attributeValue.Value.Should().Be(_validValue);
    }

    [Fact]
    public void Constructor_WhenProductVariantIdIsEmpty_ShouldThrowArgumentException()
    {
        Action act = () => new VariantAttributeValue(Guid.Empty, _validAttributeId, _validValue);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*ProductVariantId cannot be empty.*");
    }

    [Fact]
    public void Constructor_WhenProductAttributeIdIsEmpty_ShouldThrowArgumentException()
    {
        Action act = () => new VariantAttributeValue(_validVariantId, Guid.Empty, _validValue);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*ProductAttributeId cannot be empty.*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WhenValueIsInvalid_ShouldThrowArgumentException(string? invalidValue)
    {
        Action act = () => new VariantAttributeValue(_validVariantId, _validAttributeId, invalidValue!);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Value cannot be null or whitespace.*");
    }

    #endregion

    #region 2. UpdateValue Tests

    [Fact]
    public void UpdateValue_WhenDataIsValid_ShouldUpdateValue()
    {
        var attributeValue = new VariantAttributeValue(_validVariantId, _validAttributeId, "Old-Value");

        attributeValue.UpdateValue("New-Value");

        attributeValue.Value.Should().Be("New-Value");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdateValue_WhenNewValueIsInvalid_ShouldThrowArgumentException(string? invalidValue)
    {
        var attributeValue = new VariantAttributeValue(_validVariantId, _validAttributeId, _validValue);

        Action act = () => attributeValue.UpdateValue(invalidValue!);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Value cannot be null or whitespace.*");
    }

    #endregion
}