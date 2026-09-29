using FluentValidation.TestHelper;
using OmniMart.Application.Features.Users.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Users.Commands;

public class ReactivateUserCommandValidatorTests
{
    private readonly ReactivateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenTargetUserIdIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new ReactivateUserCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenTargetUserIdIsEmpty_ShouldHaveError()
    {
        var command = new ReactivateUserCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TargetUserId);
    }
}