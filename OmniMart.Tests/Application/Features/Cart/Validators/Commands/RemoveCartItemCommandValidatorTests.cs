using FluentValidation.TestHelper;
using OmniMart.Application.Features.Cart.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Cart.Commands;

public class RemoveCartItemCommandValidatorTests
{
    private readonly RemoveCartItemCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new RemoveCartItemCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var command = new RemoveCartItemCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ProductVariantId);
    }
}