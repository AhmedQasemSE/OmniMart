using FluentValidation.TestHelper;
using OmniMart.Application.Features.Reviews.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Reviews.Validators.Commands;

public class AddReviewCommandValidatorTests
{
    private readonly AddReviewCommandValidator _validator = new();

    private AddReviewCommand CreateValidCommand()
    {
        return new AddReviewCommand(Guid.NewGuid(), 5, "Excellent product!");
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenProductIdIsEmpty_ShouldHaveError()
    {
        var command = CreateValidCommand() with { ProductId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Validate_WhenRatingIsOutOfBounds_ShouldHaveError(int invalidRating)
    {
        var command = CreateValidCommand() with { Rating = invalidRating };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Rating);
    }

    [Fact]
    public void Validate_WhenCommentExceedsMaximumLength_ShouldHaveError()
    {
        var command = CreateValidCommand() with { Comment = new string('A', 1001) }; 
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Comment);
    }
}