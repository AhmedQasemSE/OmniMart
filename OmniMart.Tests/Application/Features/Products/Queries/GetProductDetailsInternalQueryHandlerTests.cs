using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Products.Queries;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using OmniMart.Tests.Builders;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Products.Queries;

public class GetProductDetailsInternalQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _contextMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<ILogger<GetProductDetailsInternalQueryHandler>> _loggerMock;

    private readonly GetProductDetailsInternalQueryHandler _handler;

    private readonly Guid _validUserId = Guid.NewGuid();
    private readonly VendorProfile _validVendor;

    public GetProductDetailsInternalQueryHandlerTests()
    {
        _contextMock = new Mock<IAppDbContext>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _loggerMock = new Mock<ILogger<GetProductDetailsInternalQueryHandler>>();

        _handler = new GetProductDetailsInternalQueryHandler(
            _contextMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);

        _validVendor = new VendorProfile(_validUserId, "V-123", "Test Store", 0m, "Desc");
    }

    private void SetupAuth(string? userId, string? role)
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.Role).Returns(role);
    }

    private void SetupDatabase(List<VendorProfile> vendors, List<Product> products)
    {
        _contextMock.Setup(c => c.VendorProfiles).Returns(vendors.BuildMockDbSet().Object);
        _contextMock.Setup(c => c.Products).Returns(products.BuildMockDbSet().Object);
    }

    [Fact]
    public async Task Handle_WhenUserTokenIsInvalid_ShouldReturnUnauthorized()
    {
        SetupAuth(null, null);
        var query = new GetProductDetailsInternalQuery(Guid.NewGuid());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("Invalid user token");
    }

    [Fact]
    public async Task Handle_WhenProductDoesNotExist_ShouldReturnNotFound()
    {
        SetupAuth(_validUserId.ToString(), SystemRole.Vendor.ToString());
        SetupDatabase(new List<VendorProfile> { _validVendor }, new List<Product>());

        var query = new GetProductDetailsInternalQuery(Guid.NewGuid());

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotAdminAndHasNoVendorProfile_ShouldReturnUnauthorized()
    {
        SetupAuth(_validUserId.ToString(), SystemRole.Vendor.ToString());

        var product = new ProductBuilder().WithVendorProfile(_validVendor).Build();

        SetupDatabase(new List<VendorProfile>(), new List<Product> { product });

        var query = new GetProductDetailsInternalQuery(product.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("permission");
    }

    [Fact]
    public async Task Handle_WhenUserIsVendorButNotProductOwner_ShouldReturnUnauthorized()
    {
        SetupAuth(_validUserId.ToString(), SystemRole.Vendor.ToString());

        var otherVendor = new VendorProfile(Guid.NewGuid(), "V-999", "Other Store", 0m, "Desc");

        var otherProduct = new ProductBuilder().WithVendorProfile(otherVendor).Build();

        SetupDatabase(new List<VendorProfile> { _validVendor, otherVendor }, new List<Product> { otherProduct });

        var query = new GetProductDetailsInternalQuery(otherProduct.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_WhenUserIsAdmin_ShouldReturnProductDetails()
    {
        SetupAuth(Guid.NewGuid().ToString(), SystemRole.Admin.ToString());

        var vendor = new VendorProfile(Guid.NewGuid(), "V-AdminTest", "Admin Store", 0m, "Desc");

        var product = new ProductBuilder()
            .WithVendorProfile(vendor)
            .WithName("Admin Secret Product")
            .Build();

        SetupDatabase(new List<VendorProfile> { vendor }, new List<Product> { product });

        var query = new GetProductDetailsInternalQuery(product.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(product.Id);
        result.Value.Name.Should().Be("Admin Secret Product");
    }

    [Fact]
    public async Task Handle_WhenUserIsOwnerVendor_ShouldReturnCorrectMappedDetails()
    {
        SetupAuth(_validUserId.ToString(), SystemRole.Vendor.ToString());

        var product = new ProductBuilder()
            .WithVendorProfile(_validVendor)
            .WithName("My Awesome Product")
            .WithStatus(ProductStatus.Active)
            .Build();

        SetupDatabase(new List<VendorProfile> { _validVendor }, new List<Product> { product });

        var query = new GetProductDetailsInternalQuery(product.Id);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var dto = result.Value!;
        dto.Id.Should().Be(product.Id);
        dto.VendorId.Should().Be(_validVendor.Id);
        dto.StoreName.Should().Be(_validVendor.StoreName);
        dto.Name.Should().Be("My Awesome Product");
        dto.Status.Should().Be(ProductStatus.Active);

        dto.Variants.Should().NotBeNull();
    }
}