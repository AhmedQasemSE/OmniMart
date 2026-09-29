using FluentValidation.TestHelper;
using OmniMart.Application.Features.Users.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Users.Commands;

public class SuspendUserCommandValidatorTests
{
    private readonly SuspendUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new SuspendUserCommand(Guid.NewGuid(), "Violated the terms of service.");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenTargetUserIdIsEmpty_ShouldHaveError()
    {
        var command = new SuspendUserCommand(Guid.Empty, "Violated the terms of service.");
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TargetUserId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("bad")] 
    public void Validate_WhenReasonIsInvalid_ShouldHaveError(string? invalidReason)
    {
        var command = new SuspendUserCommand(Guid.NewGuid(), invalidReason!);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }
}