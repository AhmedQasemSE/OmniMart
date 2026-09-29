using FluentValidation.TestHelper;
using OmniMart.Application.Features.Vendors.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Vendors.Commands;

public class UpdateVendorCommissionRateCommandValidatorTests
{
    private readonly UpdateVendorCommissionRateCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new UpdateVendorCommissionRateCommand(Guid.NewGuid(), 15m, new byte[] { 1, 2 });
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenVendorIdIsEmpty_ShouldHaveError()
    {
        var command = new UpdateVendorCommissionRateCommand(Guid.Empty, 15m, new byte[] { 1, 2 });
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.VendorId);
    }

    [Theory]
    [InlineData(-1)] 
    [InlineData(101)] 
    public void Validate_WhenNewRateIsOutOfBounds_ShouldHaveError(decimal invalidRate)
    {
        var command = new UpdateVendorCommissionRateCommand(Guid.NewGuid(), invalidRate, new byte[] { 1, 2 });
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NewRate);
    }
}