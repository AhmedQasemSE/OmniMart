using FluentValidation.TestHelper;
using OmniMart.Application.Features.Users.Commands;
using OmniMart.Domain.Enums;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Users.Commands;

public class ChangeUserRoleCommandValidatorTests
{
    private readonly ChangeUserRoleCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new ChangeUserRoleCommand(Guid.NewGuid(), SystemRole.Manager);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenTargetUserIdIsEmpty_ShouldHaveError()
    {
        var command = new ChangeUserRoleCommand(Guid.Empty, SystemRole.Manager);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TargetUserId);
    }

    [Fact]
    public void Validate_WhenNewRoleIsInvalidEnum_ShouldHaveError()
    {
        var command = new ChangeUserRoleCommand(Guid.NewGuid(), (SystemRole)999);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NewRole);
    }
}