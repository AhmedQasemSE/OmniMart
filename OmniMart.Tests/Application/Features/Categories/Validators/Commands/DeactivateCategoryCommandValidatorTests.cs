using FluentValidation.TestHelper;
using OmniMart.Application.Features.Categories.Commands;
using System;
using Xunit;
using static OmniMart.Application.Features.Categories.Commands.DeactivateCategoryCommandHandler;

namespace OmniMart.Tests.Application.Features.Categories.Commands;

public class DeactivateCategoryCommandValidatorTests
{
    private readonly DeactivateCategoryCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new DeactivateCategoryCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var command = new DeactivateCategoryCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}