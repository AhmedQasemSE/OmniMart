using FluentValidation.TestHelper;
using OmniMart.Application.Features.Orders.Commands;
using System;
using Xunit;
using static OmniMart.Application.Features.Orders.Commands.CompletePaymentCommandHandler;

namespace OmniMart.Tests.Application.Features.Orders.Validators;

public class CompletePaymentCommandValidatorTests
{
    private readonly CompletePaymentCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new CompletePaymentCommand(Guid.NewGuid(), "stripe_session_123");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenPaymentGroupIdIsEmpty_ShouldHaveError()
    {
        var command = new CompletePaymentCommand(Guid.Empty, "stripe_session_123");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PaymentGroupId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenStripeSessionIdIsEmpty_ShouldHaveError(string? invalidSessionId)
    {
        var command = new CompletePaymentCommand(Guid.NewGuid(), invalidSessionId!);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.StripeSessionId);
    }
}