using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Attributes.Commands;
using OmniMart.Application.Features.Attributes.Queries;
using OmniMart.Controllers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Controllers;

public class AttributesControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly AttributesController _controller;

    public AttributesControllerTests()
    {
        _controller = new AttributesController(_mediatorMock.Object);
    }

    #region 1. Commands Tests

    [Fact]
    public async Task CreateAttribute_WhenSuccessful_ShouldReturnOk()
    {
        var command = new CreateProductAttributeCommand("Size");
        var expectedId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateProductAttributeCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<Guid>.Success(expectedId));

        var result = await _controller.CreateAttribute(command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(expectedId);
    }

    [Fact]
    public async Task UpdateAttribute_WhenSuccessful_ShouldReturnOk()
    {
        var attributeId = Guid.NewGuid();
        var command = new UpdateProductAttributeCommand(attributeId, "Color", new byte[] { 1, 2, 3 });

        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateProductAttributeCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.UpdateAttribute(attributeId, command, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task DeleteAttribute_WhenSuccessful_ShouldReturnOk()
    {
        var attributeId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteProductAttributeCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Success(true));

        var result = await _controller.DeleteAttribute(attributeId, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task DeleteAttribute_WhenInUse_ShouldReturnConflict409()
    {
        var attributeId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(It.IsAny<DeleteProductAttributeCommand>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<bool>.Failure("Attribute is in use.", ErrorType.Conflict));

        var result = await _controller.DeleteAttribute(attributeId, CancellationToken.None);

        var conflictResult = result as ConflictObjectResult;
        conflictResult.Should().NotBeNull();
        conflictResult!.StatusCode.Should().Be(409);
    }

    #endregion

    #region 2. Queries Tests

    [Fact]
    public async Task GetAllAttributes_WhenSuccessful_ShouldReturnOk()
    {
        var expectedResult = new PaginatedResult<ProductAttributeDto>(new List<ProductAttributeDto>(), 0, 1, 10);

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllProductAttributesQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Result<PaginatedResult<ProductAttributeDto>>.Success(expectedResult));

        var result = await _controller.GetAllAttributes(1, 10, CancellationToken.None);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedResult);
    }

    #endregion
}