using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Reviews.Commands;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Tests.Builders;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Reviews.Commands;

public class UpdateReviewCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ILogger<UpdateReviewCommandHandler>> _loggerMock = new();

    private readonly Mock<IProductReviewRepository> _reviewRepoMock = new();
    private readonly Mock<ICustomerProfileRepository> _customerRepoMock = new();

    private readonly UpdateReviewCommandHandler _handler;

    private readonly Guid _defaultUserId = Guid.NewGuid();
    private readonly Guid _defaultCustomerId = Guid.NewGuid();
    private readonly Guid _defaultReviewId = Guid.NewGuid();
    private readonly UpdateReviewCommand _defaultCommand;
    private readonly ProductReview _defaultReview;
    private readonly CustomerProfile _defaultCustomer;

    public UpdateReviewCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.ProductReviews).Returns(_reviewRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.CustomerProfiles).Returns(_customerRepoMock.Object);

        _handler = new UpdateReviewCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _currentUserServiceMock.Object);

        _defaultCommand = new UpdateReviewCommand(_defaultReviewId, 4, "Updated comment!");

        _defaultCustomer = new CustomerProfileBuilder()
            .WithId(_defaultCustomerId)
            .WithUserId(_defaultUserId)
            .Build();

        _defaultReview = new ProductReviewBuilder()
            .WithId(_defaultReviewId)
            .WithCustomer(_defaultCustomer)
            .WithRating(5)
            .Build();
    }

    #region Helper Methods

    private void SetupDefaultSuccessBehavior()
    {
        _currentUserServiceMock.Setup(u => u.UserId).Returns(_defaultUserId.ToString());

        _customerRepoMock.Setup(r => r.GetCustomerIdByUserIdAsync(_defaultUserId, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(_defaultCustomerId);

        _reviewRepoMock.Setup(r => r.GetByIdAsync(_defaultReviewId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(_defaultReview);
    }

    #endregion

    #region Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-valid-guid")]
    public async Task Handle_WhenUserIdIsInvalid_ShouldReturnUnauthorized(string? invalidUserId)
    {
        SetupDefaultSuccessBehavior();
        _currentUserServiceMock.Setup(u => u.UserId).Returns(invalidUserId);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCustomerProfileNotFound_ShouldReturnNotFound()
    {
        SetupDefaultSuccessBehavior();
        _customerRepoMock.Setup(r => r.GetCustomerIdByUserIdAsync(_defaultUserId, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((Guid?)null);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenReviewNotFound_ShouldReturnNotFound()
    {
        SetupDefaultSuccessBehavior();
        _reviewRepoMock.Setup(r => r.GetByIdAsync(_defaultReviewId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync((ProductReview?)null);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenCustomerTriesToUpdateOtherCustomerReview_ShouldReturnUnauthorized()
    {
        SetupDefaultSuccessBehavior();

        var otherCustomer = new CustomerProfileBuilder().WithId(Guid.NewGuid()).Build();
        var anotherReview = new ProductReviewBuilder()
            .WithId(_defaultReviewId)
            .WithCustomer(otherCustomer)
            .Build();

        _reviewRepoMock.Setup(r => r.GetByIdAsync(_defaultReviewId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(anotherReview);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task Handle_WhenArgumentOutOfRangeExceptionThrown_ShouldReturnValidationFailure()
    {
        SetupDefaultSuccessBehavior();

        var invalidCommand = new UpdateReviewCommand(_defaultReviewId, 10, "Invalid rating");

        var result = await _handler.Handle(invalidCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.ErrorMessage.Should().Contain("Rating must be between 1 and 5");
    }

    [Fact]
    public async Task Handle_WhenValidData_ShouldUpdateReviewAndSave()
    {
        SetupDefaultSuccessBehavior();

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _defaultReview.Rating.Should().Be(4);
        _defaultReview.Comment.Should().Be("Updated comment!");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}