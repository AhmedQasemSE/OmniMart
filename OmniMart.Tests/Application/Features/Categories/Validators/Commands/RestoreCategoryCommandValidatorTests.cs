using FluentValidation.TestHelper;
using OmniMart.Application.Features.Categories.Commands;
using System;
using Xunit;
using static OmniMart.Application.Features.Categories.Commands.RestoreCategoryCommandHandler;

namespace OmniMart.Tests.Application.Features.Categories.Commands;

public class RestoreCategoryCommandValidatorTests
{
    private readonly RestoreCategoryCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new RestoreCategoryCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var command = new RestoreCategoryCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}