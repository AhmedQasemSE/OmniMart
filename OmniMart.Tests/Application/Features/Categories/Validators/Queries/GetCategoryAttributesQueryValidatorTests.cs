using FluentValidation.TestHelper;
using OmniMart.Application.Features.Categories.Queries;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Categories.Validators.Queries;

public class GetCategoryAttributesQueryValidatorTests
{
    private readonly GetCategoryAttributesQueryValidator _validator;

    public GetCategoryAttributesQueryValidatorTests()
    {
        _validator = new GetCategoryAttributesQueryValidator();
    }

    [Fact]
    public void Validate_WhenIdIsValid_ShouldNotHaveAnyErrors()
    {
        var query = new GetCategoryAttributesQuery(Guid.NewGuid());
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var query = new GetCategoryAttributesQuery(Guid.Empty);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }
}