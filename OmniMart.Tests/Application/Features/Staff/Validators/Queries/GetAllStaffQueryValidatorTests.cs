using FluentValidation.TestHelper;
using OmniMart.Application.Features.Staff.Queries;
using OmniMart.Domain.Enums;
using Xunit;

namespace OmniMart.Tests.Application.Features.Staff.Validators;

public class GetAllStaffQueryValidatorTests
{
    private readonly GetAllStaffQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenQueryIsValid_ShouldNotHaveAnyErrors()
    {
        var query = new GetAllStaffQuery(Page: 1, PageSize: 10, DepartmentFilter: Department.Finance);
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenPageIsInvalid_ShouldHaveError(int invalidPage)
    {
        var query = new GetAllStaffQuery(Page: invalidPage);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_WhenPageSizeIsInvalid_ShouldHaveError(int invalidPageSize)
    {
        var query = new GetAllStaffQuery(PageSize: invalidPageSize);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Validate_WhenDepartmentFilterIsInvalidEnum_ShouldHaveError()
    {
        var query = new GetAllStaffQuery(DepartmentFilter: (Department)999);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.DepartmentFilter);
    }
}