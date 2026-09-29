using FluentValidation.TestHelper;
using OmniMart.Application.Features.Products.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Validators;

public class RemoveProductImageCommandValidatorTests
{
    private readonly RemoveProductImageCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new RemoveProductImageCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdsAreEmpty_ShouldHaveErrors()
    {
        var command = new RemoveProductImageCommand(Guid.Empty, Guid.Empty);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ProductId);
        result.ShouldHaveValidationErrorFor(x => x.ImageId);
    }
}