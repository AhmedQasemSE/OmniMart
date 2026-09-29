using FluentValidation.TestHelper;
using OmniMart.Application.Features.Vendors.Queries;
using Xunit;

namespace OmniMart.Tests.Application.Features.Vendors.Queries;

public class GetAllVendorsQueryValidatorTests
{
    private readonly GetAllVendorsQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenQueryIsValid_ShouldNotHaveAnyErrors()
    {
        var query = new GetAllVendorsQuery(Page: 1, PageSize: 10);
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_WhenPageIsInvalid_ShouldHaveError(int invalidPage)
    {
        var query = new GetAllVendorsQuery(Page: invalidPage);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_WhenPageSizeIsInvalid_ShouldHaveError(int invalidPageSize)
    {
        var query = new GetAllVendorsQuery(PageSize: invalidPageSize);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }
}