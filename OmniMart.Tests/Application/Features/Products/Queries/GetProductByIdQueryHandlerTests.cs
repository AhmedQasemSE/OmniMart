using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Products.Queries.GetProductById;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Entities;
using OmniMart.Tests.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Queries;

public class GetProductByIdQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _contextMock;
    private readonly Mock<ILogger<GetProductByIdQueryHandler>> _loggerMock;
    private readonly GetProductByIdQueryHandler _handler;

    public GetProductByIdQueryHandlerTests()
    {
        _contextMock = new Mock<IAppDbContext>();
        _loggerMock = new Mock<ILogger<GetProductByIdQueryHandler>>();
        _handler = new GetProductByIdQueryHandler(_contextMock.Object, _loggerMock.Object);
    }

    private void SetupProductsDbSet(params Product[] products)
    {
        var mockDbSet = products.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.Products).Returns(mockDbSet.Object);
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldReturnNotFound()
    {
        
        SetupProductsDbSet();
        var query = new GetProductByIdQuery(Guid.NewGuid());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenProductExists_ShouldReturnProductDetailsWithVariants()
    {
        var targetProduct = new ProductBuilder()
            .WithVariant("SKU-TEST-123", 150m, 10)
            .Build();

        SetupProductsDbSet(targetProduct);
        var query = new GetProductByIdQuery(targetProduct.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        result.Value!.Id.Should().Be(targetProduct.Id);
        result.Value.Name.Should().Be(targetProduct.Name);
        result.Value.BasePrice.Should().Be(targetProduct.BasePrice);

        result.Value.Variants.Should().HaveCount(1);
        var firstVariant = result.Value.Variants.First();
        firstVariant.SKU.Should().Be("SKU-TEST-123");
        firstVariant.Price.Should().Be(150m);
        firstVariant.StockQuantity.Should().Be(10);
    }
}