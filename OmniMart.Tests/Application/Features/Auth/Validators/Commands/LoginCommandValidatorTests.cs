using FluentValidation.TestHelper;
using OmniMart.Application.Features.Auth.Command;
using Xunit;

namespace OmniMart.Tests.Application.Features.Auth.Validators.Commands;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new LoginCommand("test@omnimart.com", "Password123");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid-email-format")]
    public void Validate_WhenEmailIsInvalid_ShouldHaveError(string? invalidEmail)
    {
        var command = new LoginCommand(invalidEmail!, "Password123");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WhenPasswordIsEmpty_ShouldHaveError(string? emptyPassword)
    {
        var command = new LoginCommand("test@omnimart.com", emptyPassword!);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}