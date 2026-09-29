using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Cart.Commands;
using OmniMart.Application.Features.Cart.Queries;
using OmniMart.Controllers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Controllers;

public class CartControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly CartController _controller;

    public CartControllerTests()
    {
        _controller = new CartController(_mediatorMock.Object);
    }

    #region 1. Add & Update Tests

    [Fact]
    public async Task AddToCart_WhenSuccessful_ShouldReturnOk()
    {
        var command = new AddToCartCommand(Guid.NewGuid(), 2);
        var expectedCartId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<AddToCartCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<Guid>.Success(expectedCartId));

        var result = await _controller.AddToCart(command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(expectedCartId);
    }

    [Fact]
    public async Task UpdateCartItemQuantity_WhenSuccessful_ShouldReturnOk()
    {
        var variantId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateCartItemQuantityCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.UpdateCartItemQuantity(variantId, 5, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    #endregion

    #region 2. Delete Tests

    [Fact]
    public async Task RemoveCartItem_WhenSuccessful_ShouldReturnOk()
    {
        var variantId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveCartItemCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.RemoveCartItem(variantId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ClearCart_WhenSuccessful_ShouldReturnOk()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<ClearCartCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.ClearCart(CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    #endregion

    #region 3. Query Tests

    [Fact]
    public async Task GetActiveCart_WhenSuccessful_ShouldReturnOk()
    {
        var expectedCart = new ActiveCartDto(Guid.NewGuid(), new List<CartItemDto>(), 150m);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveCartQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<ActiveCartDto>.Success(expectedCart));

        var result = await _controller.GetActiveCart(CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedCart);
    }

    [Fact]
    public async Task GetActiveCart_WhenCustomerNotFound_ShouldReturnNotFound404()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetActiveCartQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<ActiveCartDto>.Failure("Customer profile not found.", ErrorType.NotFound));

        var result = await _controller.GetActiveCart(CancellationToken.None);

        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
    }

    #endregion
}