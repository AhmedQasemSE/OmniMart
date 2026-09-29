using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Orders.Commands;
using OmniMart.Application.Features.Orders.Queries;
using OmniMart.Controllers;
using OmniMart.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Controllers;

public class OrderControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly OrderController _controller;

    public OrderControllerTests()
    {
        _controller = new OrderController(_mediatorMock.Object);
    }

    #region (Customer Orders)

    [Fact]
    public async Task GetCustomerOrders_WhenSuccessful_ShouldReturnOk()
    {
        var expectedResult = new PaginatedResult<CustomerOrderSummaryDto>(new List<CustomerOrderSummaryDto>(), 0, 1, 10);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCustomerOrdersQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<PaginatedResult<CustomerOrderSummaryDto>>.Success(expectedResult));

        var result = await _controller.GetCustomerOrders(1, 10, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetOrderDetails_WhenSuccessful_ShouldReturnOk()
    {
        var orderId = Guid.NewGuid();
        var address = new AddressDto("City", "Street", "123", "050");
        var expectedResult = new OrderDetailsDto(orderId, "Pending", 150m, DateTimeOffset.UtcNow, null, address, new byte[] { 1 }, new List<OrderItemDetailDto>());

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCustomerOrderDetailsQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<OrderDetailsDto>.Success(expectedResult));

        var result = await _controller.GetOrderDetails(orderId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetOrderDetails_WhenNotFound_ShouldReturnNotFound404()
    {
        var orderId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCustomerOrderDetailsQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<OrderDetailsDto>.Failure("Order not found.", ErrorType.NotFound));

        var result = await _controller.GetOrderDetails(orderId, CancellationToken.None);

        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Checkout_WhenSuccessful_ShouldReturnOk()
    {
        var command = new CheckoutCommand(Guid.NewGuid(), "CreditCard", null);
        var expectedResponse = new CheckoutResponse(new List<Guid> { Guid.NewGuid() }, "https://stripe.com/pay");

        _mediatorMock.Setup(m => m.Send(It.IsAny<CheckoutCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<CheckoutResponse>.Success(expectedResponse));

        var result = await _controller.Checkout(command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    #endregion

    #region  (Vendor Orders)

    [Fact]
    public async Task GetVendorOrders_WhenSuccessful_ShouldReturnOk()
    {
        var expectedResult = new PaginatedResult<VendorOrderSummaryDto>(new List<VendorOrderSummaryDto>(), 0, 1, 10);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetVendorOrdersQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<PaginatedResult<VendorOrderSummaryDto>>.Success(expectedResult));

        var result = await _controller.GetVendorOrders(OrderStatus.Pending, 1, 10, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetVendorOrderDetails_WhenSuccessful_ShouldReturnOk()
    {
        var orderId = Guid.NewGuid();
        var address = new AddressDto("City", "Street", "123", "050");
        var expectedResult = new VendorOrderDetailsDto(orderId, "Pending", 150m, DateTimeOffset.UtcNow, "CreditCard", address, new byte[] { 1 }, new List<OrderItemDetailDto>());

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetVendorOrderDetailsQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<VendorOrderDetailsDto>.Success(expectedResult));

        var result = await _controller.GetVendorOrderDetails(orderId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    #endregion

    #region  (Admin Orders)

    [Fact]
    public async Task GetAdminOrders_WhenSuccessful_ShouldReturnOk()
    {
        var query = new GetAdminOrdersQuery(null, null, null, null, 1, 10);
        var expectedResult = new PaginatedResult<AdminOrderSummaryDto>(new List<AdminOrderSummaryDto>(), 0, 1, 10);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAdminOrdersQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<PaginatedResult<AdminOrderSummaryDto>>.Success(expectedResult));

        var result = await _controller.GetAdminOrders(query, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    #endregion

    #region  (Order Processing)

    [Fact]
    public async Task ProcessOrder_WhenSuccessful_ShouldReturnOk()
    {
        var orderId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3 };

        _mediatorMock.Setup(m => m.Send(It.IsAny<ProcessOrderCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.ProcessOrder(orderId, rowVersion, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ShipOrder_WhenSuccessful_ShouldReturnOk()
    {
        var orderId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3 };

        _mediatorMock.Setup(m => m.Send(It.IsAny<ShipOrderCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.ShipOrder(orderId, rowVersion, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task DeliverOrder_WhenSuccessful_ShouldReturnOk()
    {
        var orderId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3 };

        _mediatorMock.Setup(m => m.Send(It.IsAny<DeliverOrderCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.DeliverOrder(orderId, rowVersion, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task CancelOrder_WhenSuccessful_ShouldReturnOk()
    {
        var orderId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3 };

        _mediatorMock.Setup(m => m.Send(It.IsAny<CancelOrderCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.CancelOrder(orderId, rowVersion, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task RefundOrder_WhenSuccessful_ShouldReturnOk()
    {
        var orderId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3 };

        _mediatorMock.Setup(m => m.Send(It.IsAny<RefundOrderCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.RefundOrder(orderId, rowVersion, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    #endregion
}