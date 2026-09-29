using FluentValidation.TestHelper;
using OmniMart.Application.Features.Orders.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Orders.Validators;

public class CancelOrderCommandValidatorTests
{
    private readonly CancelOrderCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new CancelOrderCommand(Guid.NewGuid(), new byte[] { 1, 2, 3 });
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOrderIdIsEmpty_ShouldHaveError()
    {
        var command = new CancelOrderCommand(Guid.Empty, new byte[] { 1, 2, 3 });
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OrderId);
    }

    [Fact]
    public void Validate_WhenRowVersionIsEmpty_ShouldHaveError()
    {
        var command = new CancelOrderCommand(Guid.NewGuid(), Array.Empty<byte>());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RowVersion);
    }
}