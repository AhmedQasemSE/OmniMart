using FluentValidation.TestHelper;
using OmniMart.Application.Features.Categories.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Categories.Validators.Commands;

public class UpdateCategoryCommandValidatorTests
{
    private readonly UpdateCategoryCommandValidator _validator;

    public UpdateCategoryCommandValidatorTests()
    {
        _validator = new UpdateCategoryCommandValidator();
    }

    private UpdateCategoryCommand CreateValidCommand()
    {
        return new UpdateCategoryCommand(
            CategoryId: Guid.NewGuid(),
            ParentCategoryId: null,
            Name: "Valid Name",
            Description: "Valid Description",
            RowVersion: new byte[] { 1, 2, 3 }
        );
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = CreateValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenCategoryIdIsEmpty_ShouldHaveError()
    {
        var command = CreateValidCommand() with { CategoryId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenNameIsInvalid_ShouldHaveError(string? invalidName)
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
    public void Validate_WhenDescriptionIsInvalid_ShouldHaveError(string? invalidDesc)
    {
        var command = CreateValidCommand() with { Description = invalidDesc! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WhenDescriptionExceedsMaximumLength_ShouldHaveError()
    {
        var longDesc = new string('A', 501);
        var command = CreateValidCommand() with { Description = longDesc };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WhenParentCategoryIdEqualsCategoryId_ShouldHaveError()
    {
        var categoryId = Guid.NewGuid();
        var command = CreateValidCommand() with { CategoryId = categoryId, ParentCategoryId = categoryId };

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ParentCategoryId);
    }
}