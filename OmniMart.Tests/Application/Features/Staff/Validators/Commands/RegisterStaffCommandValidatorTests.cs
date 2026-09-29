using FluentValidation.TestHelper;
using OmniMart.Application.Features.Staff.Commands;
using OmniMart.Domain.Enums;
using Xunit;

namespace OmniMart.Tests.Application.Features.Staff.Validators;

public class ChangeUserRoleCommandValidatorTests
{
    private readonly RegisterStaffCommandValidator _validator = new();

    private RegisterStaffCommand CreateValidCommand()
    {
        return new RegisterStaffCommand("Samir", "Nassar", "samir@omnimart.com", "+905555555555", "StrongPass123!", Department.HR, SystemRole.Manager, 7500m);
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
        var command = new RegisterStaffCommand(emptyValue!, emptyValue!, emptyValue!, emptyValue!, emptyValue!, Department.HR, SystemRole.Manager, 7500m);
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FirstName);
        result.ShouldHaveValidationErrorFor(x => x.LastName);
        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Validate_WhenSalaryIsInvalid_ShouldHaveError(decimal invalidSalary)
    {
        var command = CreateValidCommand() with { Salary = invalidSalary };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Salary);
    }

    [Theory]
    [InlineData(SystemRole.Customer)]
    [InlineData(SystemRole.Vendor)]
    public void Validate_WhenRoleIsInvalidForStaff_ShouldHaveError(SystemRole invalidRole)
    {
        var command = CreateValidCommand() with { Role = invalidRole };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }
}