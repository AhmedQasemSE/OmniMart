using FluentValidation.TestHelper;
using OmniMart.Application.Features.Cart.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Cart.Commands;

public class UpdateCartItemQuantityCommandValidatorTests
{
    private readonly UpdateCartItemQuantityCommandValidator _validator = new();

    private UpdateCartItemQuantityCommand CreateValidCommand()
    {
        return new UpdateCartItemQuantityCommand(Guid.NewGuid(), 10);
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenProductVariantIdIsEmpty_ShouldHaveError()
    {
        var command = CreateValidCommand() with { ProductVariantId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ProductVariantId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(51)] 
    public void Validate_WhenNewQuantityIsInvalid_ShouldHaveError(int invalidQuantity)
    {
        var command = CreateValidCommand() with { NewQuantity = invalidQuantity };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NewQuantity);
    }
}