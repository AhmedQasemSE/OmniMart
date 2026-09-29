using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Entities;
using OmniMart.Infrastructure.Services.Queries;
using OmniMart.Tests.Builders;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Infrastructure.Services.Queries;

public class ProductOutOfStockEvenQueriesTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly ProductOutOfStockEventsQueries _queries;

    public ProductOutOfStockEvenQueriesTests()
    {
        _queries = new ProductOutOfStockEventsQueries(_contextMock.Object);
    }

    #region Helper Methods

    private void SetupVariants(params ProductVariant[] variants)
    {
        var mockDbSet = variants.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.ProductVariants).Returns(mockDbSet.Object);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task GetProductOutOfStockEvent_WhenVariantExists_ShouldReturnMappedDetails()
    {
        var vendorUser = new UserBuilder().WithEmail("vendor-oos@test.com").Build();
        var vendorProfile = new VendorProfileBuilder().WithUser(vendorUser).Build();

        var product = new ProductBuilder()
            .WithName("Popular Item")
            .WithVendorProfile(vendorProfile)
            .Build();

        var variant = new ProductVariantBuilder()
            .WithProductEntity(product)
            .WithSKU("SKU-OOS-123")
            .Build();

        SetupVariants(variant);

        var result = await _queries.GetProductOutOfStockEvent(variant.Id);

        result.Should().NotBeNull();
        result!.VendorEmail.Should().Be("vendor-oos@test.com");
        result.ProductName.Should().Be("Popular Item");
        result.VariantSKU.Should().Be("SKU-OOS-123");
    }

    [Fact]
    public async Task GetProductOutOfStockEvent_WhenVariantDoesNotExist_ShouldReturnNull()
    {
        SetupVariants();

        var result = await _queries.GetProductOutOfStockEvent(Guid.NewGuid());

        result.Should().BeNull();
    }

    #endregion
}