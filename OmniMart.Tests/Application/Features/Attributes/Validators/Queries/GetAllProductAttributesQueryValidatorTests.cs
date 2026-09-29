using FluentValidation.TestHelper;
using OmniMart.Application.Features.Attributes.Queries;
using Xunit;

namespace OmniMart.Tests.Application.Features.Attributes.Validators.Queries;

public class GetAllProductAttributesQueryValidatorTests
{
    private readonly GetAllProductAttributesQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenQueryIsValid_ShouldNotHaveAnyErrors()
    {
        var query = new GetAllProductAttributesQuery(PageNumber: 1, PageSize: 10);
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenPageNumberIsInvalid_ShouldHaveError(int invalidPage)
    {
        var query = new GetAllProductAttributesQuery(PageNumber: invalidPage, PageSize: 10);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.PageNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)] 
    public void Validate_WhenPageSizeIsInvalid_ShouldHaveError(int invalidPageSize)
    {
        var query = new GetAllProductAttributesQuery(PageNumber: 1, PageSize: invalidPageSize);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }
}