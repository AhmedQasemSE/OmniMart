using FluentValidation.TestHelper;
using OmniMart.Application.Features.Orders.Queries;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Orders.Validators;

public class GetAdminOrdersQueryValidatorTests
{
    private readonly GetAdminOrdersQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenQueryIsValid_ShouldNotHaveAnyErrors()
    {
        var query = new GetAdminOrdersQuery(Page: 1, PageSize: 10);
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenPageIsInvalid_ShouldHaveError(int invalidPage)
    {
        var query = new GetAdminOrdersQuery(Page: invalidPage);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)] 
    public void Validate_WhenPageSizeIsInvalid_ShouldHaveError(int invalidPageSize)
    {
        var query = new GetAdminOrdersQuery(PageSize: invalidPageSize);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Validate_WhenStartDateIsAfterEndDate_ShouldHaveError()
    {
        var query = new GetAdminOrdersQuery(
            StartDate: DateTimeOffset.UtcNow.AddDays(1),
            EndDate: DateTimeOffset.UtcNow
        );
        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x);
    }
}