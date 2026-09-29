using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Reviews.Commands;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using OmniMart.Tests.Builders;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Reviews.Commands;

public class DeleteReviewCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ILogger<DeleteReviewCommandHandler>> _loggerMock = new();

    private readonly Mock<IProductReviewRepository> _reviewRepoMock = new();
    private readonly Mock<ICustomerProfileRepository> _customerRepoMock = new();

    private readonly DeleteReviewCommandHandler _handler;

    private readonly Guid _defaultUserId = Guid.NewGuid();
    private readonly Guid _defaultCustomerId = Guid.NewGuid();
    private readonly Guid _defaultReviewId = Guid.NewGuid();
    private readonly DeleteReviewCommand _defaultCommand;
    private readonly ProductReview _defaultReview;

    public DeleteReviewCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.ProductReviews).Returns(_reviewRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.CustomerProfiles).Returns(_customerRepoMock.Object);

        _handler = new DeleteReviewCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _currentUserServiceMock.Object);

        _defaultCommand = new DeleteReviewCommand(_defaultReviewId);

        var customer = new CustomerProfileBuilder().WithId(_defaultCustomerId).WithUserId(_defaultUserId).Build();

        _defaultReview = new ProductReviewBuilder()
            .WithId(_defaultReviewId)
            .WithCustomer(customer)
            .Build();
    }

    #region Helper Methods

    private void SetupDefaultSuccessBehavior()
    {
        _currentUserServiceMock.Setup(u => u.UserId).Returns(_defaultUserId.ToString());
        _currentUserServiceMock.Setup(u => u.Role).Returns(SystemRole.Customer.ToString());

        _reviewRepoMock.Setup(r => r.GetByIdAsync(_defaultReviewId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(_defaultReview);

        _customerRepoMock.Setup(r => r.GetCustomerIdByUserIdAsync(_defaultUserId, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(_defaultCustomerId);
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
    public async Task Handle_WhenCustomerTriesToDeleteOtherCustomerReview_ShouldReturnUnauthorized()
    {
        SetupDefaultSuccessBehavior();
        _customerRepoMock.Setup(r => r.GetCustomerIdByUserIdAsync(_defaultUserId, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(Guid.NewGuid());

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        _reviewRepoMock.Verify(r => r.Delete(It.IsAny<ProductReview>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAdminDeletesAnyReview_ShouldSucceed()
    {
        SetupDefaultSuccessBehavior();

        _currentUserServiceMock.Setup(u => u.Role).Returns(SystemRole.Customer.ToString());

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _reviewRepoMock.Verify(r => r.Delete(_defaultReview), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOwnerCustomerDeletesReview_ShouldSucceed()
    {
        SetupDefaultSuccessBehavior();

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _reviewRepoMock.Verify(r => r.Delete(_defaultReview), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}