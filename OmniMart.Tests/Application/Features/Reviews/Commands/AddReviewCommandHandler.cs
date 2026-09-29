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

public class AddReviewCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<ILogger<AddReviewCommandHandler>> _loggerMock = new();

    private readonly Mock<ICustomerProfileRepository> _customerRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IProductReviewRepository> _reviewRepoMock = new();

    private readonly AddReviewCommandHandler _handler;

    private readonly Guid _defaultUserId = Guid.NewGuid();
    private readonly Guid _defaultCustomerId = Guid.NewGuid();
    private readonly Guid _defaultProductId = Guid.NewGuid();
    private readonly AddReviewCommand _defaultCommand;
    private readonly Product _defaultProduct;

    public AddReviewCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.CustomerProfiles).Returns(_customerRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.ProductReviews).Returns(_reviewRepoMock.Object);

        _handler = new AddReviewCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _currentUserServiceMock.Object);

        _defaultCommand = new AddReviewCommand(_defaultProductId, 5, "Great product!");

        _defaultProduct = new ProductBuilder().WithId(_defaultProductId).Build();
    }

    #region Helper Methods

    private void SetupDefaultSuccessBehavior()
    {
        _currentUserServiceMock.Setup(u => u.UserId).Returns(_defaultUserId.ToString());

        _customerRepoMock.Setup(r => r.GetCustomerIdByUserIdAsync(_defaultUserId, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(_defaultCustomerId);

        _productRepoMock.Setup(r => r.GetByIdAsync(_defaultProductId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(_defaultProduct);

        _reviewRepoMock.Setup(r => r.HasCustomerReviewedProductAsync(_defaultProductId, _defaultCustomerId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(false);
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
        result.ErrorMessage.Should().Contain("Customer profile not found.");

    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldReturnNotFound()
    {
        SetupDefaultSuccessBehavior();
        _productRepoMock.Setup(r => r.GetByIdAsync(_defaultProductId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Product?)null);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
        result.ErrorMessage.Should().Contain("Product not found.");
    }

    [Fact]
    public async Task Handle_WhenCustomerAlreadyReviewedProduct_ShouldReturnConflict()
    {
        SetupDefaultSuccessBehavior();
        _reviewRepoMock.Setup(r => r.HasCustomerReviewedProductAsync(_defaultProductId, _defaultCustomerId, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(true);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenInvalidOperationExceptionThrown_ShouldReturnFailure()
    {
        SetupDefaultSuccessBehavior();
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new InvalidOperationException("Simulated domain domain rule violation"));

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public async Task Handle_WhenAllDataIsValid_ShouldAddReviewAndSave()
    {
        SetupDefaultSuccessBehavior();

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _defaultProduct.Reviews.Should().HaveCount(1);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}