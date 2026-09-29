using FluentValidation.TestHelper;
using OmniMart.Application.Features.Vendors.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Vendors.Commands;

public class ApproveVendorCommandValidatorTests
{
    private readonly ApproveVendorCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new ApproveVendorCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenVendorIdIsEmpty_ShouldHaveError()
    {
        var command = new ApproveVendorCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.VendorId);
    }
}