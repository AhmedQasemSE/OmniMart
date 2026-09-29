using FluentValidation.TestHelper;
using OmniMart.Application.Features.Attributes.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Attributes.Validators.Commands;

public class UpdateProductAttributeCommandValidatorTests
{
    private readonly UpdateProductAttributeCommandValidator _validator = new();

    private UpdateProductAttributeCommand CreateValidCommand()
    {
        return new UpdateProductAttributeCommand(Guid.NewGuid(), "Valid Name", new byte[] { 1, 2 });
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var command = CreateValidCommand() with { Id = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenNewNameIsInvalid_ShouldHaveError(string? invalidName)
    {
        var command = CreateValidCommand() with { NewName = invalidName! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NewName);
    }

    [Fact]
    public void Validate_WhenNewNameExceedsMaximumLength_ShouldHaveError()
    {
        var command = CreateValidCommand() with { NewName = new string('A', 101) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.NewName);
    }

    [Fact]
    public void Validate_WhenRowVersionIsEmpty_ShouldHaveError()
    {
        var command = CreateValidCommand() with { RowVersion = Array.Empty<byte>() };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RowVersion);
    }
}