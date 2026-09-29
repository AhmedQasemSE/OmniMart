using FluentValidation.TestHelper;
using OmniMart.Application.Features.Customers.Commands;
using Xunit;

namespace OmniMart.Tests.Application.Features.Customers.Validators;

public class RedeemLoyaltyPointsCommandValidatorTests
{
    private readonly RedeemLoyaltyPointsCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new RedeemLoyaltyPointsCommand(50);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Validate_WhenPointsAreInvalid_ShouldHaveError(int invalidPoints)
    {
        var command = new RedeemLoyaltyPointsCommand(invalidPoints);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PointsToRedeem);
    }
}