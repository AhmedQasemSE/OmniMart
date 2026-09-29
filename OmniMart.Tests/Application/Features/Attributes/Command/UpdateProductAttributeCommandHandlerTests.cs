using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Attributes.Commands;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Attributes.Command;

public class UpdateProductAttributeCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<UpdateProductAttributeCommandHandler>> _loggerMock;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly UpdateProductAttributeCommandHandler _handler;

    public UpdateProductAttributeCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<UpdateProductAttributeCommandHandler>>();
        _cacheMock = new Mock<IDistributedCache>(); 

        _handler = new UpdateProductAttributeCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_WhenAttributeDoesNotExist_ShouldReturnNotFound()
    {
        var command = new UpdateProductAttributeCommand(Guid.NewGuid(), "NewName", new byte[] { 1 });

        _unitOfWorkMock.Setup(u => u.ProductAttributes.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenNewNameAlreadyExists_ShouldReturnConflict()
    {
        var command = new UpdateProductAttributeCommand(Guid.NewGuid(), " Existing Name ", new byte[] { 1 });
        var existingAttribute = new ProductAttribute("Old Name");

        _unitOfWorkMock.Setup(u => u.ProductAttributes.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAttribute);

        _unitOfWorkMock.Setup(u => u.ProductAttributes.IsNameExistsAsync("Existing Name", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("already exists");
    }

    [Fact]
    public async Task Handle_WhenConcurrencyConflictOccurs_ShouldReturnConflict()
    {
        var command = new UpdateProductAttributeCommand(Guid.NewGuid(), "New Name", new byte[] { 1 });
        var existingAttribute = new ProductAttribute("Old Name");

        _unitOfWorkMock.Setup(u => u.ProductAttributes.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAttribute);

        _unitOfWorkMock.Setup(u => u.ProductAttributes.IsNameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("modified by another user");
    }

    [Fact]
    public async Task Handle_WhenValidRequest_ShouldUpdateAndClearCache()
    {
        var command = new UpdateProductAttributeCommand(Guid.NewGuid(), "  Clean Name  ", new byte[] { 1, 2, 3 });
        var existingAttribute = new ProductAttribute("Old Name");

        _unitOfWorkMock.Setup(u => u.ProductAttributes.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAttribute);

        _unitOfWorkMock.Setup(u => u.ProductAttributes.IsNameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        existingAttribute.Name.Should().Be("Clean Name");

        _unitOfWorkMock.Verify(u => u.ProductAttributes.SetOriginalRowVersion(existingAttribute, command.RowVersion), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _cacheMock.Verify(c => c.RemoveAsync($"{CacheKeys.ProductAttributesPrefix}1_Size_10", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDomainValidationFails_ShouldReturnValidationFailure()
    {
        var command = new UpdateProductAttributeCommand(Guid.NewGuid(), "", new byte[] { 1 });
        var existingAttribute = new ProductAttribute("Old Name");

        _unitOfWorkMock.Setup(u => u.ProductAttributes.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAttribute);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}