using FluentValidation.TestHelper;
using OmniMart.Application.Features.Attributes.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Attributes.Validators.Commands;

public class CreateProductAttributeCommandValidatorTests
{
    private readonly CreateProductAttributeCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new CreateProductAttributeCommand("Color");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")] 
    public void Validate_WhenNameIsInvalid_ShouldHaveError(string? invalidName)
    {
        var command = new CreateProductAttributeCommand(invalidName!);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaximumLength_ShouldHaveError()
    {
        var command = new CreateProductAttributeCommand(new string('A', 101)); 
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}