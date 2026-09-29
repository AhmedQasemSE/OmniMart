using FluentValidation.TestHelper;
using OmniMart.Application.Features.CustomerProfiles.Commands;
using Xunit;

namespace OmniMart.Tests.Application.Features.Customers.Validators;

public class AddCustomerAddressCommandValidatorTests
{
    private readonly AddCustomerAddressCommandValidator _validator = new();

    private AddCustomerAddressCommand CreateValidCommand()
    {
        return new AddCustomerAddressCommand("Home", "Istanbul", "Sisli", "34000", "05555555555");
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
        var command = new AddCustomerAddressCommand(emptyValue!, emptyValue!, emptyValue!, "34000", emptyValue!);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title);
        result.ShouldHaveValidationErrorFor(x => x.City);
        result.ShouldHaveValidationErrorFor(x => x.Street);
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }
}