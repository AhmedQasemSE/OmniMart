using FluentValidation.TestHelper;
using OmniMart.Application.Features.Orders.Queries;
using System;
using Xunit;
using static OmniMart.Application.Features.Orders.Queries.GetCustomerOrderDetailsQueryHandler;

namespace OmniMart.Tests.Application.Features.Orders.Validators;

public class GetCustomerOrderDetailsQueryValidatorTests
{
    private readonly GetCustomerOrderDetailsQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsValid_ShouldNotHaveAnyErrors()
    {
        var query = new GetCustomerOrderDetailsQuery(Guid.NewGuid());
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var query = new GetCustomerOrderDetailsQuery(Guid.Empty);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.OrderId);
    }
}