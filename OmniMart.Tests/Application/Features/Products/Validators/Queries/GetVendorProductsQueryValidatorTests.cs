using FluentValidation.TestHelper;
using OmniMart.Application.Features.Products.Queries;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Queries;

public class GetVendorProductsQueryValidatorTests
{
    private readonly GetVendorProductsQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenQueryIsValid_ShouldNotHaveAnyErrors()
    {
        var query = new GetVendorProductsQuery(Status: null, IsDeleted: false, Page: 1, PageSize: 10);
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenPageIsInvalid_ShouldHaveError(int invalidPage)
    {
        var query = new GetVendorProductsQuery(Status: null, Page: invalidPage);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_WhenPageSizeIsInvalid_ShouldHaveError(int invalidPageSize)
    {
        var query = new GetVendorProductsQuery(Status: null, PageSize: invalidPageSize);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }
}