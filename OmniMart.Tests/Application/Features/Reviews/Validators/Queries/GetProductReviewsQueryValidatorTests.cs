using FluentValidation.TestHelper;
using OmniMart.Application.Features.Reviews.Queries;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Reviews.Validators;

public class GetProductReviewsQueryValidatorTests
{
    private readonly GetProductReviewsQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenQueryIsValid_ShouldNotHaveAnyErrors()
    {
        var query = new GetProductReviewsQuery(Guid.NewGuid(), 1, 10);
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenProductIdIsEmpty_ShouldHaveError()
    {
        var query = new GetProductReviewsQuery(Guid.Empty);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenPageIsInvalid_ShouldHaveError(int invalidPage)
    {
        var query = new GetProductReviewsQuery(Guid.NewGuid(), Page: invalidPage, PageSize: 10);
        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)] 
    public void Validate_WhenPageSizeIsInvalid_ShouldHaveError(int invalidPageSize)
    {
        var query = new GetProductReviewsQuery(Guid.NewGuid(), Page: 1, PageSize: invalidPageSize);
        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }
}