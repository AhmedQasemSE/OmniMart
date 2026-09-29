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

public class ProductSuspendedQueriesTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly ProductSuspendedQueries _queries;

    public ProductSuspendedQueriesTests()
    {
        _queries = new ProductSuspendedQueries(_contextMock.Object);
    }

    #region Helper Methods

    private void SetupProducts(params Product[] products)
    {
        var mockDbSet = products.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.Products).Returns(mockDbSet.Object);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task GetProductAndVendorDetailsAsync_WhenProductExists_ShouldReturnMappedDetails()
    {
        var vendorUser = new UserBuilder().WithEmail("vendor-suspended@test.com").Build();
        var vendorProfile = new VendorProfileBuilder().WithUser(vendorUser).Build();

        var product = new ProductBuilder()
            .WithName("Suspended Item")
            .WithVendorProfile(vendorProfile)
            .Build();

        SetupProducts(product);

        var result = await _queries.GetProductAndVendorDetailsAsync(product.Id, vendorProfile.Id);

        result.Should().NotBeNull();
        result!.ProductName.Should().Be("Suspended Item");
        result.VendorEmail.Should().Be("vendor-suspended@test.com");
    }

    [Fact]
    public async Task GetProductAndVendorDetailsAsync_WhenProductDoesNotExist_ShouldReturnNull()
    {
        SetupProducts();

        var result = await _queries.GetProductAndVendorDetailsAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeNull();
    }

    #endregion
}