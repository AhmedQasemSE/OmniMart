using FluentValidation.TestHelper;
using OmniMart.Application.Features.Products.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Validators;

public class AddProductVariantCommandValidatorTests
{
    private readonly AddProductVariantCommandValidator _validator = new();

    private AddProductVariantCommand CreateValidCommand() => new AddProductVariantCommand(Guid.NewGuid(), "SKU-123", 100m, 10);

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenProductIdIsEmpty_ShouldHaveError()
    {
        var command = CreateValidCommand() with { ProductId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WhenSKUIsEmpty_ShouldHaveError(string? invalidSku)
    {
        var command = CreateValidCommand() with { SKU = invalidSku! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SKU);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-50.5)]
    public void Validate_WhenPriceOrStockIsNegative_ShouldHaveError(decimal negativeValue)
    {
        var command = CreateValidCommand() with { Price = negativeValue, StockQuantity = (int)negativeValue };
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Price);
        result.ShouldHaveValidationErrorFor(x => x.StockQuantity);
    }
}