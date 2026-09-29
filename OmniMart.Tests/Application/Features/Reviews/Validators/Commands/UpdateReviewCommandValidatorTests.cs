using FluentValidation.TestHelper;
using OmniMart.Application.Features.Reviews.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Reviews.Validators.Commands;

public class UpdateReviewCommandValidatorTests
{
    private readonly UpdateReviewCommandValidator _validator = new();

    private UpdateReviewCommand CreateValidCommand()
    {
        return new UpdateReviewCommand(Guid.NewGuid(), 4, "Good product, but delayed shipping.");
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenReviewIdIsEmpty_ShouldHaveError()
    {
        var command = CreateValidCommand() with { ReviewId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ReviewId);
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
}