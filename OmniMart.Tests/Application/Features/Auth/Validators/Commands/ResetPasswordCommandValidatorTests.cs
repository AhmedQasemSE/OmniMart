using FluentValidation.TestHelper;
using OmniMart.Application.Features.Auth.Command;
using Xunit;

namespace OmniMart.Tests.Application.Features.Auth.Validators.Commands;

public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    private ResetPasswordCommand CreateValidCommand()
    {
        return new ResetPasswordCommand("test@omnimart.com", "123456", "NewPass123", "NewPass123");
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
    [InlineData("invalid-email")]
    public void Validate_WhenEmailIsInvalid_ShouldHaveError(string? invalidEmail)
    {
        var command = CreateValidCommand() with { Email = invalidEmail! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")] 
    [InlineData("1234567")] 
    public void Validate_WhenTokenIsInvalid_ShouldHaveError(string? invalidToken)
    {
        var command = CreateValidCommand() with { Token = invalidToken! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")] 
    public void Validate_WhenNewPasswordIsInvalid_ShouldHaveError(string? invalidPassword)
    {
        var command = CreateValidCommand() with { NewPassword = invalidPassword! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_WhenConfirmPasswordDoesNotMatch_ShouldHaveError()
    {
        var command = CreateValidCommand() with { ConfirmPassword = "DifferentPassword123" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword); 
    }
}