using FluentValidation.TestHelper;
using OmniMart.Application.Features.CustomerProfiles.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Customers.Validators;

public class DeleteCustomerAddressCommandValidatorTests
{
    private readonly DeleteCustomerAddressCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new DeleteCustomerAddressCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var command = new DeleteCustomerAddressCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AddressId);
    }
}