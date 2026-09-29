using FluentValidation.TestHelper;
using OmniMart.Application.Features.Auth.Command;
using Xunit;

namespace OmniMart.Tests.Application.Features.Auth.Validators.Commands;

public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new RefreshTokenCommand("valid-long-secret-refresh-token");
        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenRefreshTokenIsInvalid_ShouldHaveError(string? invalidToken)
    {
        var command = new RefreshTokenCommand(invalidToken!);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }
}