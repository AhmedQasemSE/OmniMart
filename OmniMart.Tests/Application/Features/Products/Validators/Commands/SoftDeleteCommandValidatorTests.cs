using FluentValidation.TestHelper;
using OmniMart.Application.Features.Products.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Validators;

public class SoftDeleteCommandValidatorTests
{
    private readonly SoftDeleteCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new SoftDeleteCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var command = new SoftDeleteCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.id);
    }
}