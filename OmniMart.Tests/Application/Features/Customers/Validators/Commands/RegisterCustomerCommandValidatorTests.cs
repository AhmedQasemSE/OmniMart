using FluentValidation.TestHelper;
using OmniMart.Application.Features.Customers.Commands;
using Xunit;

namespace OmniMart.Tests.Application.Features.Customers.Validators;

public class RegisterCustomerCommandValidatorTests
{
    private readonly RegisterCustomerCommandValidator _validator = new();

    private RegisterCustomerCommand CreateValidCommand()
    {
        return new RegisterCustomerCommand("Ahmed", "Yaseen", "test@omnimart.com", "+905555555555", "StrongPass123!");
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WhenFieldsAreEmpty_ShouldHaveErrors(string? emptyValue)
    {
        var command = new RegisterCustomerCommand(emptyValue!, emptyValue!, emptyValue!, emptyValue!, emptyValue!);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FirstName);
        result.ShouldHaveValidationErrorFor(x => x.LastName);
        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_WhenEmailIsInvalid_ShouldHaveError()
    {
        var command = CreateValidCommand() with { Email = "invalid-email" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}