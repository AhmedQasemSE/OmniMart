using FluentValidation.TestHelper;
using OmniMart.Application.Features.CustomerProfiles.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Customers.Validators;

public class UpdateCustomerAddressCommandValidatorTests
{
    private readonly UpdateCustomerAddressCommandValidator _validator = new();

    private UpdateCustomerAddressCommand CreateValidCommand()
    {
        return new UpdateCustomerAddressCommand(Guid.NewGuid(), "Work", "Ankara", "Cankaya", "06000", "05555555555");
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenAddressIdIsEmpty_ShouldHaveError()
    {
        var command = CreateValidCommand() with { AddressId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AddressId);
    }
}