using FluentValidation.TestHelper;
using OmniMart.Application.Features.Vendors.Commands;
using Xunit;

namespace OmniMart.Tests.Application.Features.Vendors.Commands;

public class UpdateVendorProfileCommandValidatorTests
{
    private readonly UpdateVendorProfileCommandValidator _validator = new();

    private UpdateVendorProfileCommand CreateValidCommand()
    {
        return new UpdateVendorProfileCommand("Super Store", "123456789");
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
        var command = new UpdateVendorProfileCommand(emptyValue!, emptyValue!);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.StoreName);
        result.ShouldHaveValidationErrorFor(x => x.CommercialRegisterNumber);
    }

    [Fact]
    public void Validate_WhenStoreNameExceedsMaximumLength_ShouldHaveError()
    {
        var command = CreateValidCommand() with { StoreName = new string('A', 101) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.StoreName);
    }

    [Fact]
    public void Validate_WhenCommercialRegisterNumberContainsLetters_ShouldHaveError()
    {
        var command = CreateValidCommand() with { CommercialRegisterNumber = "12345ABC" }; 
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CommercialRegisterNumber);
    }
}