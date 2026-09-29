using FluentValidation.TestHelper;
using OmniMart.Application.Features.Products.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Validators;

public class UpdateProductVariantCommandValidatorTests
{
    private readonly UpdateProductVariantCommandValidator _validator = new();

    private UpdateProductVariantCommand CreateValidCommand() => new UpdateProductVariantCommand(Guid.NewGuid(), "SKU-123", 150m, 20);

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(-10)]
    public void Validate_WhenPriceOrStockIsNegative_ShouldHaveError(decimal negativeValue)
    {
        var command = CreateValidCommand() with { Price = negativeValue, StockQuantity = (int)negativeValue };
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Price);
        result.ShouldHaveValidationErrorFor(x => x.StockQuantity);
    }
}