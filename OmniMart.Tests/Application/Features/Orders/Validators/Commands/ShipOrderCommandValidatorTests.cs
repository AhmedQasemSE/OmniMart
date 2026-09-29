using FluentValidation.TestHelper;
using OmniMart.Application.Features.Orders.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Orders.Validators;

public class ShipOrderCommandValidatorTests
{
    private readonly ShipOrderCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new ShipOrderCommand(Guid.NewGuid(), new byte[] { 1 });
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOrderIdIsEmpty_ShouldHaveError()
    {
        var command = new ShipOrderCommand(Guid.Empty, new byte[] { 1 });
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OrderId);
    }
}