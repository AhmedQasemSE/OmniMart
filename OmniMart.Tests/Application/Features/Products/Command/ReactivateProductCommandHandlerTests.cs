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

public class ReactivateProductCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<ReactivateProductCommandHandler>> _loggerMock;
    private readonly Mock<IProductRepository> _productRepoMock;

    private readonly ReactivateProductCommandHandler _handler;

    private readonly Guid _validProductId = Guid.NewGuid();

    public ReactivateProductCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<ReactivateProductCommandHandler>>();
        _productRepoMock = new Mock<IProductRepository>();

        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);

        _handler = new ReactivateProductCommandHandler(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    #region Helper Methods

    private ReactivateProductCommand CreateValidCommand()
    {
        return new ReactivateProductCommand(_validProductId);
    }

    private void SetupValidContext()
    {
        var suspendedProduct = new ProductBuilder()
            .WithId(_validProductId)
            .WithStatus(ProductStatus.Suspended)
            .Build();

        _productRepoMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(suspendedProduct);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_WhenProductIsSuspended_ShouldReactivateAndSave()
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
    [InlineData(ProductStatus.PendingReview)]
    [InlineData(ProductStatus.Active)]
    [InlineData(ProductStatus.Rejected)]
    public async Task Handle_WhenProductIsNotSuspended_ShouldReturnConflict(ProductStatus invalidStatus)
    {
        SetupValidContext();

        var invalidProduct = new ProductBuilder()
            .WithId(_validProductId)
            .WithStatus(invalidStatus)
            .Build();

        _productRepoMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidProduct);

        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}