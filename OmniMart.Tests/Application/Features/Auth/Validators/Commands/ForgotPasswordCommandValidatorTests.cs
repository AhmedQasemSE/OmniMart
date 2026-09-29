using FluentValidation.TestHelper;
using OmniMart.Application.Features.Auth.Commands.ForgotPassword;
using Xunit;

namespace OmniMart.Tests.Application.Features.Auth.Validators.Commands;

public class ForgotPasswordCommandValidatorTests
{
    private readonly ForgotPasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenEmailIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new ForgotPasswordCommand("test@omnimart.com");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid-email")]
    public void Validate_WhenEmailIsInvalid_ShouldHaveError(string? invalidEmail)
    {
        var command = new ForgotPasswordCommand(invalidEmail!);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}