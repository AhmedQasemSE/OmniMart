using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Attributes.Commands;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Attributes.Commands;

public class CreateProductAttributeCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IProductAttributeRepository> _attributeRepoMock;
    private readonly Mock<ILogger<CreateProductAttributeCommandHandler>> _loggerMock;
    private readonly CreateProductAttributeCommandHandler _handler;

    public CreateProductAttributeCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _attributeRepoMock = new Mock<IProductAttributeRepository>();
        _loggerMock = new Mock<ILogger<CreateProductAttributeCommandHandler>>();

        _unitOfWorkMock.Setup(u => u.ProductAttributes).Returns(_attributeRepoMock.Object);

        _handler = new CreateProductAttributeCommandHandler(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyExists_ShouldReturnConflict()
    {
        var command = new CreateProductAttributeCommand(" Color ");
        var cleanName = "Color";

        _attributeRepoMock.Setup(repo => repo.IsNameExistsAsync(cleanName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("already exists");

        _attributeRepoMock.Verify(repo => repo.AddAsync(It.IsAny<ProductAttribute>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDomainValidationFails_ShouldReturnValidationFailure()
    {
        var command = new CreateProductAttributeCommand("");

        _attributeRepoMock.Setup(repo => repo.IsNameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNameIsUnique_ShouldCreateAttributeAndReturnSuccess()
    {
        var command = new CreateProductAttributeCommand("  Storage Capacity  ");
        var expectedCleanName = "Storage Capacity";

        _attributeRepoMock.Setup(repo => repo.IsNameExistsAsync(expectedCleanName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty(); 

        _attributeRepoMock.Verify(repo => repo.AddAsync(
            It.Is<ProductAttribute>(a => a.Name == expectedCleanName),
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}