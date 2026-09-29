using FluentValidation.TestHelper;
using OmniMart.Application.Features.Products.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Validators;

public class UpdateProductCommandValidatorTests
{
    private readonly UpdateProductCommandValidator _validator = new();

    private UpdateProductCommand CreateValidCommand()
    {
        return new UpdateProductCommand(Guid.NewGuid(), "Updated Phone", "New Specs", 1200m, Guid.NewGuid(), new byte[] { 1, 2 });
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdsAreEmpty_ShouldHaveErrors()
    {
        var command = CreateValidCommand() with { ProductId = Guid.Empty, CategoryId = Guid.Empty };
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ProductId);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void Validate_WhenBasePriceIsNegative_ShouldHaveError()
    {
        var command = CreateValidCommand() with { BasePrice = -10m };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BasePrice);
    }
}