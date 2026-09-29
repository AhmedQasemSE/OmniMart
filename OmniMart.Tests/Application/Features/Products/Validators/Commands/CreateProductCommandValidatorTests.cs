using FluentValidation.TestHelper;
using OmniMart.Application.Features.Products.Commands;
using System;
using System.Collections.Generic;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Validators;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    private CreateProductCommand CreateValidCommand()
    {
        return new CreateProductCommand(
            CategoryId: Guid.NewGuid(),
            Name: "Smartphone",
            Description: "Latest model",
            BasePrice: 1000m,
            Variants: new List<VariantRequestDto>
            {
                new VariantRequestDto("SKU-1", 1000m, 10),
                new VariantRequestDto("SKU-2", 1100m, 5)
            }
        );
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WhenRequiredStringsAreEmpty_ShouldHaveErrors(string? emptyValue)
    {
        var command = CreateValidCommand() with { Name = emptyValue!, Description = emptyValue! };
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WhenCategoryIdIsEmpty_ShouldHaveError()
    {
        var command = CreateValidCommand() with { CategoryId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Validate_WhenBasePriceIsInvalid_ShouldHaveError(decimal invalidPrice)
    {
        var command = CreateValidCommand() with { BasePrice = invalidPrice };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.BasePrice);
    }

    [Fact]
    public void Validate_WhenVariantsListIsEmpty_ShouldHaveError()
    {
        var command = CreateValidCommand() with { Variants = new List<VariantRequestDto>() };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Variants);
    }

    [Fact]
    public void Validate_WhenVariantsContainDuplicateSKUs_ShouldHaveError()
    {
        var command = CreateValidCommand() with
        {
            Variants = new List<VariantRequestDto>
            {
                new VariantRequestDto("DUPLICATE-SKU", 100m, 10),
                new VariantRequestDto("DUPLICATE-SKU", 200m, 5) 
            }
        };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Variants);
    }
}