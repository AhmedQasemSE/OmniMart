using FluentAssertions;
using OmniMart.Domain.Entities;
using System;
using Xunit;

namespace OmniMart.Tests.Domain;

public class ProductImageTests
{
    private readonly Guid _validProductId = Guid.NewGuid();

    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateSuccessfully()
    {
        var image = new ProductImage(_validProductId, "http://image.url", "pub-id-123", true);

        image.Id.Should().NotBeEmpty();
        image.ProductId.Should().Be(_validProductId);
        image.ImageUrl.Should().Be("http://image.url");
        image.PublicId.Should().Be("pub-id-123");
        image.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WhenProductIdIsEmpty_ShouldThrowArgumentException()
    {
        Action act = () => new ProductImage(Guid.Empty, "url", "pub-id");

        act.Should().Throw<ArgumentException>().WithMessage("*Product ID is required.*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WhenImageUrlIsInvalid_ShouldThrowArgumentException(string? invalidUrl)
    {
        Action act = () => new ProductImage(_validProductId, invalidUrl!, "pub-id");

        act.Should().Throw<ArgumentException>().WithMessage("*Image URL is required.*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WhenPublicIdIsInvalid_ShouldThrowArgumentException(string? invalidId)
    {
        Action act = () => new ProductImage(_validProductId, "url", invalidId!);

        act.Should().Throw<ArgumentException>().WithMessage("*Public ID is required.*");
    }

    [Fact]
    public void PrimaryStatusMethods_ShouldUpdateIsPrimaryCorrectly()
    {
        var image = new ProductImage(_validProductId, "url", "pub-id", isPrimary: false);

        image.SetAsPrimary();
        image.IsPrimary.Should().BeTrue();

        image.RemovePrimary();
        image.IsPrimary.Should().BeFalse();

        image.SetPrimary(true);
        image.IsPrimary.Should().BeTrue();
    }
}