using FluentValidation.TestHelper;
using OmniMart.Application.Features.Categories.Commands;
using System;
using Xunit;
using static OmniMart.Application.Features.Categories.Commands.ActivateCategoryCommandHandler;

namespace OmniMart.Tests.Application.Features.Categories.Commands;

public class ActivateCategoryCommandValidatorTests
{
    private readonly ActivateCategoryCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new ActivateCategoryCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var command = new ActivateCategoryCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}