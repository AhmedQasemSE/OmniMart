using FluentValidation.TestHelper;
using OmniMart.Application.Features.Vendors.Queries;
using Xunit;

namespace OmniMart.Tests.Application.Features.Vendors.Queries;

public class GetPendingVendorsQueryValidatorTests
{
    private readonly GetPendingVendorsQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenQueryIsValid_ShouldNotHaveAnyErrors()
    {
        var query = new GetPendingVendorsQuery(Page: 1, PageSize: 10);
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenPageIsInvalid_ShouldHaveError(int invalidPage)
    {
        var query = new GetPendingVendorsQuery(Page: invalidPage);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_WhenPageSizeIsInvalid_ShouldHaveError(int invalidPageSize)
    {
        var query = new GetPendingVendorsQuery(PageSize: invalidPageSize);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }
}