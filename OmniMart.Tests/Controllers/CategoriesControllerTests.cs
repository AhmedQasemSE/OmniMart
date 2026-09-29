using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Categories.Commands;
using OmniMart.Application.Features.Categories.Queries.GetAllCategories;
using OmniMart.Application.Features.Categories.Queries.GetCategoryById;
using OmniMart.Controllers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Controllers;

public class CategoryControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly CategoryController _controller;

    public CategoryControllerTests()
    {
        _controller = new CategoryController(_mediatorMock.Object);
    }

    #region 1. Create Tests

    [Fact]
    public async Task CreateCategoryAsync_WhenSuccessful_ShouldReturnOk()
    {
        var command = new CreateCategoryCommand("Electronics", "Category description", null);
        var expectedId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateCategoryCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<Guid>.Success(expectedId));

        var result = await _controller.CreateCategoryAsync(command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(expectedId);
    }

    [Fact]
    public async Task CreateCategoryAsync_WhenValidationFails_ShouldReturnUnprocessableEntity422()
    {
        var command = new CreateCategoryCommand("", "", null);

        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateCategoryCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<Guid>.Failure("Validation failed", ErrorType.Validation));

        var result = await _controller.CreateCategoryAsync(command, CancellationToken.None);

        var unprocessableResult = result as UnprocessableEntityObjectResult;
        unprocessableResult.Should().NotBeNull();
        unprocessableResult!.StatusCode.Should().Be(422);
    }

    #endregion

    #region 2. Update Tests

    [Fact]
    public async Task UpdateCategoryAsync_WhenSuccessful_ShouldReturnOk()
    {
        var categoryId = Guid.NewGuid();
        var command = new UpdateCategoryCommand(categoryId, null, "Phones", "Smartphones", new byte[] { 1, 2, 3 });

        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateCategoryCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<Guid>.Success(categoryId));

        var result = await _controller.UpdateCategoryAsync(command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(categoryId);
    }

    #endregion

    #region 3. Query Tests

    [Fact]
    public async Task GetAllCategoriesAsync_WhenSuccessful_ShouldReturnOk()
    {
        var categoriesList = new List<CategoryDto>();

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllCategoriesQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<List<CategoryDto>>.Success(categoriesList));

        var result = await _controller.GetAllCategoriesAsync(CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(categoriesList);
    }

    [Fact]
    public async Task GetCategoryByIdAsync_WhenSuccessful_ShouldReturnOk()
    {
        var categoryId = Guid.NewGuid();
        var categoryDetail = new CategoryDetailDto(categoryId, "Laptops", "Gaming Laptops", null, new byte[] { 1 });

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCategoryByIdQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<CategoryDetailDto>.Success(categoryDetail));

        var result = await _controller.GetCategoryByIdAsync(categoryId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(categoryDetail);
    }

    [Fact]
    public async Task GetCategoryByIdAsync_WhenNotFound_ShouldReturnNotFound404()
    {
        var categoryId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetCategoryByIdQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<CategoryDetailDto>.Failure("Category not found", ErrorType.NotFound));

        var result = await _controller.GetCategoryByIdAsync(categoryId, CancellationToken.None);

        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
    }

    #endregion

    #region 4. Delete & Status Tests

    [Fact]
    public async Task DeleteCategoryAsync_WhenSuccessful_ShouldReturnOk()
    {
        var categoryId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteCategoryCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.DeleteCategoryAsync(categoryId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ActivateCategoryAsync_WhenSuccessful_ShouldReturnOk()
    {
        var categoryId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<ActivateCategoryCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.ActivateCategoryAsync(categoryId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task DeactivateCategoryAsync_WhenSuccessful_ShouldReturnOk()
    {
        var categoryId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<DeactivateCategoryCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.DeactivateCategoryAsync(categoryId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task RestoreCategoryAsync_WhenSuccessful_ShouldReturnOk()
    {
        var categoryId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<RestoreCategoryCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.RestoreCategoryAsync(categoryId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ToggleCategoryStatusAsync_WhenSuccessful_ShouldReturnOk()
    {
        var categoryId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<ToggleCategoryStatusCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.ToggleCategoryStatusAsync(categoryId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    #endregion
}