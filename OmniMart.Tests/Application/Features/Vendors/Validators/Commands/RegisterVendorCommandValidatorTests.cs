using FluentValidation.TestHelper;
using OmniMart.Application.Features.Vendors.Commands;
using Xunit;

namespace OmniMart.Tests.Application.Features.Vendors.Commands;

public class RegisterVendorCommandValidatorTests
{
    private readonly RegisterVendorCommandValidator _validator = new();

    private RegisterVendorCommand CreateValidCommand()
    {
        return new RegisterVendorCommand(
            "Ahmed", "Yaseen", "vendor@omnimart.com", "0555123456", "StrongPass123!", "Tech Store", "123456789"
        );
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
    public void Validate_WhenRequiredFieldsAreEmpty_ShouldHaveErrors(string? emptyValue)
    {
        var command = new RegisterVendorCommand(emptyValue!, emptyValue!, emptyValue!, emptyValue!, emptyValue!, emptyValue!, emptyValue!);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FirstName);
        result.ShouldHaveValidationErrorFor(x => x.LastName);
        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
        result.ShouldHaveValidationErrorFor(x => x.Password);
        result.ShouldHaveValidationErrorFor(x => x.StoreName);
        result.ShouldHaveValidationErrorFor(x => x.CommercialRegisterNumber);
    }

    [Fact]
    public void Validate_WhenEmailIsInvalid_ShouldHaveError()
    {
        var command = CreateValidCommand() with { Email = "invalid-email-format" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}