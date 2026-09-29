using FluentValidation.TestHelper;
using OmniMart.Application.Features.Categories.Commands;
using System;
using Xunit;
using static OmniMart.Application.Features.Categories.Commands.DeleteCategoryCommandHandler;

namespace OmniMart.Tests.Application.Features.Categories.Validators.Commands;

public class DeleteCategoryCommandValidatorTests
{
    private readonly DeleteCategoryCommandValidator _validator;

    public DeleteCategoryCommandValidatorTests()
    {
        _validator = new DeleteCategoryCommandValidator();
    }

    [Fact]
    public void Validate_WhenIdIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new DeleteCategoryCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var command = new DeleteCategoryCommand(Guid.Empty);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}