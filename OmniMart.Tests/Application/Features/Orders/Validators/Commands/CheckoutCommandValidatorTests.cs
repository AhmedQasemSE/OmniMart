using FluentValidation.TestHelper;
using OmniMart.Application.Features.Orders.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Orders.Validators;

public class CheckoutCommandValidatorTests
{
    private readonly CheckoutCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new CheckoutCommand(Guid.NewGuid(), "CreditCard");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenCustomerAddressIdIsEmpty_ShouldHaveError()
    {
        var command = new CheckoutCommand(Guid.Empty, "CreditCard");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CustomerAddressId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenPaymentMethodIsEmpty_ShouldHaveError(string? invalidMethod)
    {
        var command = new CheckoutCommand(Guid.NewGuid(), invalidMethod!);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PaymentMethod);
    }
}