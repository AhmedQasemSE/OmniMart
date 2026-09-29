using FluentValidation.TestHelper;
using OmniMart.Application.Features.Categories.Queries.GetCategoryById;
using System;
using Xunit;
using static OmniMart.Application.Features.Categories.Queries.GetCategoryById.GetCategoryByIdQueryHandler;

namespace OmniMart.Tests.Application.Features.Categories.Queries;

public class GetCategoryByIdQueryValidatorTests
{
    private readonly GetCategoryByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsValid_ShouldNotHaveAnyErrors()
    {
        var query = new GetCategoryByIdQuery(Guid.NewGuid());
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var query = new GetCategoryByIdQuery(Guid.Empty);
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}