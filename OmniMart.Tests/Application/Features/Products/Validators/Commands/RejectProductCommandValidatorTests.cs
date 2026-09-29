using FluentValidation.TestHelper;
using OmniMart.Application.Features.Products.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Commands;

public class RejectProductCommandValidatorTests
{
    private readonly RejectProductCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new RejectProductCommand(Guid.NewGuid(), "Violates image policy.");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenProductIdIsEmpty_ShouldHaveError()
    {
        var command = new RejectProductCommand(Guid.Empty, "Violates image policy.");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Short")] 
    public void Validate_WhenReasonIsInvalid_ShouldHaveError(string? invalidReason)
    {
        var command = new RejectProductCommand(Guid.NewGuid(), invalidReason!);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }
}