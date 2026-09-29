using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Products.Commands;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using OmniMart.Tests.Builders;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Command;

public class RejectProductCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<RejectProductCommandHandler>> _loggerMock;
    private readonly Mock<IProductRepository> _productRepoMock;

    private readonly RejectProductCommandHandler _handler;

    private readonly Guid _validProductId = Guid.NewGuid();
    private readonly string _validRejectionReason = "Images are not clear and violate policy.";

    public RejectProductCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<RejectProductCommandHandler>>();
        _productRepoMock = new Mock<IProductRepository>();

        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);

        _handler = new RejectProductCommandHandler(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    #region Helper Methods

    private RejectProductCommand CreateValidCommand()
    {
        return new RejectProductCommand(_validProductId, _validRejectionReason);
    }

    private void SetupValidContext()
    {
        var pendingProduct = new ProductBuilder()
            .WithId(_validProductId)
            .WithStatus(ProductStatus.PendingReview)
            .Build();

        _productRepoMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingProduct);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_WhenProductIsPendingReview_ShouldRejectAndSave()
    {
        SetupValidContext();
        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldReturnNotFound()
    {
        SetupValidContext();
        _productRepoMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null); 

        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Theory]
    [InlineData(ProductStatus.Draft)]
    [InlineData(ProductStatus.Active)]
    [InlineData(ProductStatus.Rejected)]
    [InlineData(ProductStatus.Suspended)]
    public async Task Handle_WhenProductIsNotPendingReview_ShouldReturnValidationFailure(ProductStatus invalidStatus)
    {
        SetupValidContext();

        var invalidProduct = new ProductBuilder().WithId(_validProductId)
            .WithStatus(invalidStatus)
            .Build();

        _productRepoMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidProduct);

        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.ErrorMessage.Should().Contain("Only pending products");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}