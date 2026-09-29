using FluentValidation.TestHelper;
using OmniMart.Application.Features.Attributes.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Attributes.Validators.Commands;

public class DeleteProductAttributeCommandValidatorTests
{
    private readonly DeleteProductAttributeCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new DeleteProductAttributeCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var command = new DeleteProductAttributeCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}