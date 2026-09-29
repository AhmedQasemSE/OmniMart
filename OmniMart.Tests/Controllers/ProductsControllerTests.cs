using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Products.Commands;
using OmniMart.Application.Features.Products.Queries;
using OmniMart.Application.Features.Products.Queries.GetAdminProducts;
using OmniMart.Application.Features.Products.Queries.GetProductById;
using OmniMart.Application.Features.Products.Queries.GetProducts;
using OmniMart.Controllers;
using OmniMart.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Controllers;

public class ProductsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _controller = new ProductsController(_mediatorMock.Object);
    }

    #region  Public Endpoints 

    [Fact]
    public async Task GetProducts_WhenSuccessful_ShouldReturnOk()
    {
        var query = new GetProductsQuery(null, null, null, null, 1, 10);
        var expectedResult = new PaginatedResult<ProductSummaryDto>(new List<ProductSummaryDto>(), 0, 1, 10);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetProductsQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<PaginatedResult<ProductSummaryDto>>.Success(expectedResult));

        var result = await _controller.GetProducts(query, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetProductByIdAsync_WhenSuccessful_ShouldReturnOk()
    {
        var productId = Guid.NewGuid();
        var expectedResult = new ProductDetailDto(productId, "Phone", new byte[] { 1 }, "Smart Phone", 1000m, new List<ProductVariantDto>());

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetProductByIdQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<ProductDetailDto>.Success(expectedResult));

        var result = await _controller.GetProductByIdAsync(productId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetProductByIdAsync_WhenNotFound_ShouldReturnNotFound404()
    {
        var productId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetProductByIdQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<ProductDetailDto>.Failure("Product not found.", ErrorType.NotFound));

        var result = await _controller.GetProductByIdAsync(productId, CancellationToken.None);

        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
    }

    #endregion

    #region  Vendor Endpoints 

    [Fact]
    public async Task CreateProductAsync_WhenSuccessful_ShouldReturnOk()
    {
        var command = new CreateProductCommand(Guid.NewGuid(), "Phone", "Nice phone", 1000m, new List<VariantRequestDto> { new VariantRequestDto("SKU1", 1000m, 10) });
        var expectedId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateProductCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<Guid>.Success(expectedId));

        var result = await _controller.CreateProductAsync(command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(expectedId);
    }

    [Fact]
    public async Task UpdateProductAsync_WhenSuccessful_ShouldReturnOk()
    {
        var productId = Guid.NewGuid();
        var command = new UpdateProductCommand(productId, "Phone v2", "Updated", 1200m, Guid.NewGuid(), new byte[] { 1, 2 });

        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateProductCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<Guid>.Success(productId));

        var result = await _controller.UpdateProductAsync(productId, command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task SubmitProductForReview_WhenSuccessful_ShouldReturnOk()
    {
        var productId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<SubmitProductForReviewCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.SubmitProductForReview(productId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task SetPublishStatus_WhenSuccessful_ShouldReturnOk()
    {
        var productId = Guid.NewGuid();
        var command = new SetPublishStatusCommand(productId, true);

        _mediatorMock.Setup(m => m.Send(It.IsAny<SetPublishStatusCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.SetPublishStatus(productId, command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task DeleteProduct_WhenSuccessful_ShouldReturnOk()
    {
        var productId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<SoftDeleteCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.DeleteProduct(productId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task RestoreProduct_WhenSuccessful_ShouldReturnOk()
    {
        var productId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<RestoreProductCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.RestoreProduct(productId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetVendorProducts_WhenSuccessful_ShouldReturnOk()
    {
        var query = new GetVendorProductsQuery(ProductStatus.Active, false, 1, 10);
        var expectedResult = new PaginatedResult<VendorProductSummaryDto>(new List<VendorProductSummaryDto>(), 0, 1, 10);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetVendorProductsQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<PaginatedResult<VendorProductSummaryDto>>.Success(expectedResult));

        var result = await _controller.GetVendorProducts(query, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    #endregion

    #region  Admin Endpoints 

    [Fact]
    public async Task GetAdminProducts_WhenSuccessful_ShouldReturnOk()
    {
        var query = new GetAdminProductsQuery(null, null, null, null, 1, 10);
        var expectedResult = new PaginatedResult<AdminProductDto>(new List<AdminProductDto>(), 0, 1, 10);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAdminProductsQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<PaginatedResult<AdminProductDto>>.Success(expectedResult));

        var result = await _controller.GetAdminProducts(query, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ApproveProduct_WhenSuccessful_ShouldReturnOk()
    {
        var productId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<ApproveProductCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.ApproveProduct(productId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task RejectProduct_WhenSuccessful_ShouldReturnOk()
    {
        var productId = Guid.NewGuid();
        var command = new RejectProductCommand(productId, "Missing details");

        _mediatorMock.Setup(m => m.Send(It.IsAny<RejectProductCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.RejectProduct(productId, command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task SuspendProduct_WhenSuccessful_ShouldReturnOk()
    {
        var productId = Guid.NewGuid();
        var command = new SuspendProductCommand(productId, "Violation");

        _mediatorMock.Setup(m => m.Send(It.IsAny<SuspendProductCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.SuspendProduct(productId, command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ReactivateProduct_WhenSuccessful_ShouldReturnOk()
    {
        var productId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<ReactivateProductCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.ReactivateProduct(productId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    #endregion

    #region  Product Variants & Images Tests

    [Fact]
    public async Task AddProductVariant_WhenSuccessful_ShouldReturnOk()
    {
        var productId = Guid.NewGuid();
        var command = new AddProductVariantCommand(productId, "SKU-123", 150m, 50);

        _mediatorMock.Setup(m => m.Send(It.IsAny<AddProductVariantCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.AddProductVariant(productId, command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task UpdateProductVariant_WhenSuccessful_ShouldReturnOk()
    {
        var productId = Guid.NewGuid();
        var sku = "SKU-123";
        var command = new UpdateProductVariantCommand(productId, sku, 140m, 40);

        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateProductVariantCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.UpdateProductVariant(productId, sku, command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task RemoveProductVariant_WhenSuccessful_ShouldReturnOk()
    {
        var productId = Guid.NewGuid();
        var sku = "SKU-123";

        _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveProductVariantCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.RemoveProductVariant(productId, sku, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task AssignValuesToVariant_WhenSuccessful_ShouldReturnOk()
    {
        var productId = Guid.NewGuid();
        var sku = "SKU-123";
        var command = new AssignValuesToVariantCommand(productId, sku, new List<VariantAttributeValueDto>(), new byte[] { 1 });

        _mediatorMock.Setup(m => m.Send(It.IsAny<AssignValuesToVariantCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.AssignValuesToVariant(productId, sku, command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task UploadProductImage_WhenSuccessful_ShouldReturnOk()
    {
        var productId = Guid.NewGuid();

        var fileMock = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
        var content = "Fake image content";
        var fileName = "test.png";
        var ms = new System.IO.MemoryStream();
        var writer = new System.IO.StreamWriter(ms);
        writer.Write(content);
        writer.Flush();
        ms.Position = 0;

        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(ms.Length);

        _mediatorMock.Setup(m => m.Send(It.IsAny<UploadProductImageCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.UploadProductImage(productId, fileMock.Object, true, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task RemoveProductImage_WhenSuccessful_ShouldReturnOk()
    {
        var productId = Guid.NewGuid();
        var imageId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveProductImageCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.RemoveProductImage(productId, imageId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task SetPrimaryImage_WhenSuccessful_ShouldReturnOk()
    {
        var productId = Guid.NewGuid();
        var imageId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<SetPrimaryProductImageCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.SetPrimaryImage(productId, imageId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    #endregion

    #region  Internal & Shared Endpoints Tests

    [Fact]
    public async Task GetProductDetailsInternal_WhenSuccessful_ShouldReturnOk()
    {
        var productId = Guid.NewGuid();
        var expectedDto = new ProductDetailsInternalDto(
            productId, Guid.NewGuid(), "Store", Guid.NewGuid(), "Product", "Desc",
            100m, ProductStatus.Active, true, false, DateTimeOffset.UtcNow, null,
            null, null, new byte[] { 1 }, new List<InternalProductVariantDto>()
        );

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetProductDetailsInternalQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<ProductDetailsInternalDto>.Success(expectedDto));

        var result = await _controller.GetProductDetailsInternal(productId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedDto);
    }

    #endregion
}