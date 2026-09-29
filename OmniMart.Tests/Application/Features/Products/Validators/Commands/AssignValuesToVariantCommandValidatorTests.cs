using FluentValidation.TestHelper;
using OmniMart.Application.Features.Products.Commands;
using System;
using System.Collections.Generic;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Validators;

public class AssignValuesToVariantCommandValidatorTests
{
    private readonly AssignValuesToVariantCommandValidator _validator = new();

    private AssignValuesToVariantCommand CreateValidCommand()
    {
        return new AssignValuesToVariantCommand(
            ProductId: Guid.NewGuid(),
            SKU: "SKU-123",
            Values: new List<VariantAttributeValueDto>
            {
                new VariantAttributeValueDto(Guid.NewGuid(), "Red"),
                new VariantAttributeValueDto(Guid.NewGuid(), "XL")
            },
            RowVersion: new byte[] { 1, 2, 3 }
        );
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenValuesListIsNull_ShouldHaveError()
    {
        var command = CreateValidCommand() with { Values = null! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Values);
    }

    [Fact]
    public void Validate_WhenValuesContainDuplicateAttributes_ShouldHaveError()
    {
        var duplicateAttrId = Guid.NewGuid();
        var invalidValues = new List<VariantAttributeValueDto>
        {
            new VariantAttributeValueDto(duplicateAttrId, "Red"),
            new VariantAttributeValueDto(duplicateAttrId, "Blue") 
        };

        var command = CreateValidCommand() with { Values = invalidValues };
        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Values);
    }
}