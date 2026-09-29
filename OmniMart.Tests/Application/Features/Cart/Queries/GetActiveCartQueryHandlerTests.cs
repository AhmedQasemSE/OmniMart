using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Cart.Queries;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Tests.Builders;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Cart.Queries;

public class GetActiveCartQueryHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly Mock<ILogger<GetActiveCartQueryHandler>> _loggerMock = new();

    private readonly GetActiveCartQueryHandler _handler;
    private readonly Guid _defaultUserId = Guid.NewGuid();

    public GetActiveCartQueryHandlerTests()
    {
        _handler = new GetActiveCartQueryHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);
    }

    #region Helper Methods 

    private void SetupCurrentUser()
    {
        _currentUserServiceMock.Setup(u => u.UserId).Returns(_defaultUserId.ToString());
    }

    private void SetupCustomers(params CustomerProfile[] customers)
    {
        var mockDbSet = customers.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.CustomerProfiles).Returns(mockDbSet.Object);
    }

    private void SetupCarts(params OmniMart.Domain.Entities.Cart[] carts)
    {
        var mockDbSet = carts.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.Carts).Returns(mockDbSet.Object);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        _currentUserServiceMock.Setup(u => u.UserId).Returns(string.Empty);

        var query = new GetActiveCartQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_WhenCustomerNotFound_ShouldReturnNotFound()
    {
        SetupCurrentUser();
        SetupCustomers(); 

        var query = new GetActiveCartQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenCartNotFound_ShouldReturnEmptyCartSuccess()
    {
        SetupCurrentUser();

        var customer = new CustomerProfileBuilder()
            .WithUserId(_defaultUserId)
            .Build();

        SetupCustomers(customer);
        SetupCarts();

        var query = new GetActiveCartQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.CartId.Should().Be(Guid.Empty);
        result.Value.Items.Should().BeEmpty();
        result.Value.GrandTotal.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenCartExists_ShouldReturnPopulatedCartSuccess()
    {
        SetupCurrentUser();

        var customer = new CustomerProfileBuilder()
            .WithUserId(_defaultUserId)
            .Build();
        SetupCustomers(customer);

        var product = new ProductBuilder().WithName("Awesome Laptop").Build();

        var variant = new ProductVariantBuilder()
            .WithSKU("LAP-123")
            .WithPrice(1500m)
            .WithProductEntity(product)
            .Build();

        var cartItem = new CartItemBuilder()
            .WithProductVariantId(variant.Id)
            .WithQuantity(2)
            .WithProductVariantEntity(variant)
            .Build();

        var cart = new CartBuilder()
            .WithCustomerId(customer.Id)
            .WithCartItem(cartItem)
            .Build();

        SetupCarts(cart);

        var query = new GetActiveCartQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.CartId.Should().Be(cart.Id);

        result.Value.Items.Should().HaveCount(1);
        var returnedItem = result.Value.Items.First();
        returnedItem.Quantity.Should().Be(2);
        returnedItem.ProductName.Should().Be("Awesome Laptop");
        returnedItem.SKU.Should().Be("LAP-123");

        result.Value.GrandTotal.Should().Be(3000m); 
    }

    #endregion
}