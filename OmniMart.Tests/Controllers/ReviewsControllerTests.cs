using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Reviews.Commands;
using OmniMart.Application.Features.Reviews.Queries;
using OmniMart.Controllers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Controllers;

public class ReviewsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ReviewsController _controller;

    public ReviewsControllerTests()
    {
        _controller = new ReviewsController(_mediatorMock.Object);
    }

    #region 1. Queries Tests

    [Fact]
    public async Task GetProductReviews_WhenSuccessful_ShouldReturnOk()
    {
        var productId = Guid.NewGuid();
        var expectedSummary = new ProductReviewsSummaryDto(
            productId,
            4.5,
            1,
            new PaginatedResult<ReviewDto>(new List<ReviewDto>(), 1, 1, 10)
        );

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetProductReviewsQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<ProductReviewsSummaryDto>.Success(expectedSummary));

        var result = await _controller.GetProductReviews(productId, 1, 10, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedSummary);
    }

    #endregion

    #region 2. Commands Tests

    [Fact]
    public async Task AddReview_WhenSuccessful_ShouldReturnOk()
    {
        var command = new AddReviewCommand(Guid.NewGuid(), 5, "Great product!");
        var expectedReviewId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<AddReviewCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<Guid>.Success(expectedReviewId));

        var result = await _controller.AddReview(command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(expectedReviewId);
    }

    [Fact]
    public async Task UpdateReview_WhenSuccessful_ShouldReturnOk()
    {
        var reviewId = Guid.NewGuid();
        var command = new UpdateReviewCommand(reviewId, 4, "Updated comment");

        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateReviewCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.UpdateReview(reviewId, command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task DeleteReview_WhenSuccessful_ShouldReturnOk()
    {
        var reviewId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteReviewCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.DeleteReview(reviewId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task DeleteReview_WhenNotFound_ShouldReturnNotFound404()
    {
        var reviewId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteReviewCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Failure("Review not found.", ErrorType.NotFound));

        var result = await _controller.DeleteReview(reviewId, CancellationToken.None);

        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
    }

    #endregion
}