using FluentValidation.TestHelper;
using OmniMart.Application.Features.Staff.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Staff.Validators;

public class IncreaseStaffSalaryCommandValidatorTests
{
    private readonly IncreaseStaffSalaryCommandValidator _validator = new();

    private IncreaseStaffSalaryCommand CreateValidCommand()
    {
        return new IncreaseStaffSalaryCommand(Guid.NewGuid(), 1500m);
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenStaffUserIdIsEmpty_ShouldHaveError()
    {
        var command = CreateValidCommand() with { StaffUserId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.StaffUserId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    [InlineData(10001)] 
    public void Validate_WhenAmountIsOutOfBounds_ShouldHaveError(decimal invalidAmount)
    {
        var command = CreateValidCommand() with { Amount = invalidAmount };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }
}