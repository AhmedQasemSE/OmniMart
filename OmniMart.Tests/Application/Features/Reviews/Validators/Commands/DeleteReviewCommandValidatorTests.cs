using FluentValidation.TestHelper;
using OmniMart.Application.Features.Reviews.Commands;
using System;
using Xunit;

namespace OmniMart.Tests.Application.Features.Reviews.Validators.Commands;

public class DeleteReviewCommandValidatorTests
{
    private readonly DeleteReviewCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsValid_ShouldNotHaveAnyErrors()
    {
        var command = new DeleteReviewCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var command = new DeleteReviewCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ReviewId);
    }
}