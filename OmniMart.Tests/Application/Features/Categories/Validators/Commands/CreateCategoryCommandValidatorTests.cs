using FluentValidation.TestHelper;
using OmniMart.Application.Features.Categories.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Categories.Validators.Commands;

public class CreateCategoryCommandValidatorTests
{
    private readonly CreateCategoryCommandValidator _validator;

    public CreateCategoryCommandValidatorTests()
    {
        _validator = new CreateCategoryCommandValidator();
    }

    private CreateCategoryCommand CreateValidCommand()
    {
        return new CreateCategoryCommand("Electronics", "Tech gadgets and devices", null);
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = CreateValidCommand();

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenNameIsEmpty_ShouldHaveError(string? invalidName)
    {
        var command = CreateValidCommand() with { Name = invalidName! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaximumLength_ShouldHaveError()
    {
        var longName = new string('A', 101);
        var command = CreateValidCommand() with { Name = longName };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenDescriptionIsEmpty_ShouldHaveError(string? invalidDescription)
    {
        var command = CreateValidCommand() with { Description = invalidDescription! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WhenDescriptionExceedsMaximumLength_ShouldHaveError()
    {
        var longDescription = new string('A', 501);
        var command = CreateValidCommand() with { Description = longDescription };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}