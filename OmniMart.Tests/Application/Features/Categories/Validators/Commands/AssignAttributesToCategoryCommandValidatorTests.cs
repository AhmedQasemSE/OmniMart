using FluentValidation.TestHelper;
using OmniMart.Application.Features.Categories.Commands;
using System;
using System.Collections.Generic;
using Xunit;

namespace OmniMart.Tests.Application.Features.Categories.Validators.Commands;

public class AssignAttributesToCategoryCommandValidatorTests
{
    private readonly AssignAttributesToCategoryCommandValidator _validator;

    public AssignAttributesToCategoryCommandValidatorTests()
    {
        _validator = new AssignAttributesToCategoryCommandValidator();
    }

    private AssignAttributesToCategoryCommand CreateValidCommand()
    {
        return new AssignAttributesToCategoryCommand(
            CategoryId: Guid.NewGuid(),
            Attributes: new List<CategoryAttributeRequestDto>
            {
                new CategoryAttributeRequestDto(Guid.NewGuid(), true),
                new CategoryAttributeRequestDto(Guid.NewGuid(), false)
            },
            RowVersion: new byte[] { 1, 2, 3 }
        );
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var command = CreateValidCommand();
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenCategoryIdIsEmpty_ShouldHaveError()
    {
        var command = CreateValidCommand() with { CategoryId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void Validate_WhenRowVersionIsEmpty_ShouldHaveError()
    {
        var command = CreateValidCommand() with { RowVersion = Array.Empty<byte>() };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RowVersion);
    }

    [Fact]
    public void Validate_WhenAttributesListIsNull_ShouldHaveError()
    {
        var command = CreateValidCommand() with { Attributes = null! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Attributes);
    }

    [Fact]
    public void Validate_WhenAttributesListContainsDuplicates_ShouldHaveError()
    {
        var duplicateId = Guid.NewGuid();
        var duplicateList = new List<CategoryAttributeRequestDto>
        {
            new CategoryAttributeRequestDto(duplicateId, true),
            new CategoryAttributeRequestDto(duplicateId, false) 
        };

        var command = CreateValidCommand() with { Attributes = duplicateList };

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Attributes);
    }
}