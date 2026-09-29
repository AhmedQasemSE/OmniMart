using FluentValidation.TestHelper;
using OmniMart.Application.Features.Orders.Queries;
using Xunit;
using static OmniMart.Application.Features.Orders.Queries.GetCustomerOrdersQueryHandler;

namespace OmniMart.Tests.Application.Features.Orders.Validators;

public class GetCustomerOrdersQueryValidatorTests
{
    private readonly GetCustomerOrdersQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenQueryIsValid_ShouldNotHaveAnyErrors()
    {
        var query = new GetCustomerOrdersQuery(PageNumber: 1, PageSize: 10);
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenPageNumberIsInvalid_ShouldHaveError(int invalidPage)
    {
        var query = new GetCustomerOrdersQuery(PageNumber: invalidPage);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.PageNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_WhenPageSizeIsInvalid_ShouldHaveError(int invalidPageSize)
    {
        var query = new GetCustomerOrdersQuery(PageSize: invalidPageSize);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }
}