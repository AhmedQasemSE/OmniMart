using FluentValidation.TestHelper;
using OmniMart.Application.Features.Products.Queries.GetProductById;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Queries;

public class GetProductByIdQueryValidatorTests
{
    private readonly GetProductByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsValid_ShouldNotHaveAnyErrors()
    {
        var query = new GetProductByIdQuery(Guid.NewGuid());
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var query = new GetProductByIdQuery(Guid.Empty);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}