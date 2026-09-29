using FluentValidation.TestHelper;
using OmniMart.Application.Features.Orders.Queries;
using Xunit;

namespace OmniMart.Tests.Application.Features.Orders.Validators;

public class GetVendorOrdersQueryValidatorTests
{
    private readonly GetVendorOrdersQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenQueryIsValid_ShouldNotHaveAnyErrors()
    {
        var query = new GetVendorOrdersQuery(Status: null, PageNumber: 1, PageSize: 10);
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenPageNumberIsInvalid_ShouldHaveError(int invalidPage)
    {
        var query = new GetVendorOrdersQuery(Status: null, PageNumber: invalidPage);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.PageNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_WhenPageSizeIsInvalid_ShouldHaveError(int invalidPageSize)
    {
        var query = new GetVendorOrdersQuery(Status: null, PageSize: invalidPageSize);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }
}