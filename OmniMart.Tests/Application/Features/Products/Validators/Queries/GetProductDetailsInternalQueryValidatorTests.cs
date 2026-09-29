using FluentValidation.TestHelper;
using OmniMart.Application.Features.Products.Queries;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Queries;

public class GetProductDetailsInternalQueryValidatorTests
{
    private readonly GetProductDetailsInternalQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsValid_ShouldNotHaveAnyErrors()
    {
        var query = new GetProductDetailsInternalQuery(Guid.NewGuid());
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var query = new GetProductDetailsInternalQuery(Guid.Empty);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }
}