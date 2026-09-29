using FluentValidation.TestHelper;
using OmniMart.Application.Features.Products.Queries.GetProducts;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Queries;

public class GetProductsQueryValidatorTests
{
    private readonly GetProductsQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenQueryIsValid_ShouldNotHaveAnyErrors()
    {
        var query = new GetProductsQuery(MinPrice: 10m, MaxPrice: 100m, Page: 1, PageSize: 10);
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenMinPriceIsGreaterThanMaxPrice_ShouldHaveError()
    {
        var query = new GetProductsQuery(MinPrice: 200m, MaxPrice: 100m);
        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x);
    }

    [Theory]
    [InlineData(-5)]
    public void Validate_WhenPricesAreNegative_ShouldHaveError(decimal negativePrice)
    {
        var query = new GetProductsQuery(MinPrice: negativePrice, MaxPrice: negativePrice);
        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.MinPrice);
        result.ShouldHaveValidationErrorFor(x => x.MaxPrice);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_WhenPageIsInvalid_ShouldHaveError(int invalidPage)
    {
        var query = new GetProductsQuery(Page: invalidPage, PageSize: 10);
        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_WhenPageSizeIsInvalid_ShouldHaveError(int invalidPageSize)
    {
        var query = new GetProductsQuery(Page: 1, PageSize: invalidPageSize);
        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }
}