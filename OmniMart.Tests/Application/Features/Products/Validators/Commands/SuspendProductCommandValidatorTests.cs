using FluentValidation.TestHelper;
using OmniMart.Application.Features.Products.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Commands;

public class SuspendProductCommandValidatorTests
{
    private readonly SuspendProductCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new SuspendProductCommand(Guid.NewGuid(), "Suspended for investigation.");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var command = new SuspendProductCommand(Guid.Empty, "Suspended for investigation.");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WhenReasonIsEmpty_ShouldHaveError(string? emptyReason)
    {
        var command = new SuspendProductCommand(Guid.NewGuid(), emptyReason!);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }
}