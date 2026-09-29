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

public class ApproveProductCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<ApproveProductCommandHandler>> _loggerMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();

    private readonly ApproveProductCommandHandler _handler;
    private readonly Guid _defaultProductId = Guid.NewGuid();

    public ApproveProductCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);
        _handler = new ApproveProductCommandHandler(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    #region Helper Methods (The Happy Path)

    private void SetupValidContext()
    {
        var pendingProduct = new ProductBuilder()
            .WithId(_defaultProductId)
            .WithStatus(ProductStatus.PendingReview)
            .Build();

        _productRepoMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingProduct);
    }

    private ApproveProductCommand CreateValidCommand()
    {
        return new ApproveProductCommand(_defaultProductId);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_WhenProductIsPendingReview_ShouldApproveAndSave()
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

    [Fact]
    public async Task Handle_WhenProductIsAlreadyActive_ShouldReturnConflict()
    {
        SetupValidContext();

        var activeProduct = new ProductBuilder()
            .WithId(_defaultProductId)
            .WithStatus(ProductStatus.Active)
            .Build();

        _productRepoMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeProduct);

        var command = CreateValidCommand();
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("already active");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProductIsNotPendingReview_ShouldReturnValidationFailure()
    {
        SetupValidContext();

        var draftProduct = new ProductBuilder()
            .WithId(_defaultProductId)
            .WithStatus(ProductStatus.Draft)
            .Build();

        _productRepoMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(draftProduct);

        var command = CreateValidCommand();
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.ErrorMessage.Should().Contain("Only pending products");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}