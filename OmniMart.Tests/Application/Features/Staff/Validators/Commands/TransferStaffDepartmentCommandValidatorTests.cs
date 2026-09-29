using FluentValidation.TestHelper;
using OmniMart.Application.Features.Staff.Commands;
using OmniMart.Domain.Enums;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Staff.Validators;

public class TransferStaffDepartmentCommandValidatorTests
{
    private readonly TransferStaffDepartmentCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new TransferStaffDepartmentCommand(Guid.NewGuid(), Department.IT);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenStaffUserIdIsEmpty_ShouldHaveError()
    {
        var command = new TransferStaffDepartmentCommand(Guid.Empty, Department.IT);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.StaffUserId);
    }

    [Fact]
    public void Validate_WhenDepartmentIsInvalid_ShouldHaveError()
    {
        var command = new TransferStaffDepartmentCommand(Guid.NewGuid(), (Department)999);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NewDepartment);
    }
}