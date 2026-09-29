using FluentValidation.TestHelper;
using OmniMart.Application.Features.Orders.Queries;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Orders.Validators;

public class GetVendorOrderDetailsQueryValidatorTests
{
    private readonly GetVendorOrderDetailsQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsValid_ShouldNotHaveAnyErrors()
    {
        var query = new GetVendorOrderDetailsQuery(Guid.NewGuid());
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var query = new GetVendorOrderDetailsQuery(Guid.Empty);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.OrderId);
    }
}