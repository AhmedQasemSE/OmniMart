using FluentAssertions;
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

public class DeleteProductAttributeCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<DeleteProductAttributeCommandHandler>> _loggerMock;
    private readonly DeleteProductAttributeCommandHandler _handler;

    public DeleteProductAttributeCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<DeleteProductAttributeCommandHandler>>();

        _handler = new DeleteProductAttributeCommandHandler(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenAttributeDoesNotExist_ShouldReturnNotFound()
    {
        var command = new DeleteProductAttributeCommand(Guid.NewGuid());

        _unitOfWorkMock.Setup(u => u.ProductAttributes.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductAttribute?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task Handle_WhenAttributeIsInUse_ShouldReturnConflict()
    {
        var command = new DeleteProductAttributeCommand(Guid.NewGuid());
        var existingAttribute = new ProductAttribute("Color");

        _unitOfWorkMock.Setup(u => u.ProductAttributes.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAttribute);

        _unitOfWorkMock.Setup(u => u.ProductAttributes.IsAttributeInUseAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("linked to categories or products");

        _unitOfWorkMock.Verify(u => u.ProductAttributes.Delete(It.IsAny<ProductAttribute>()), Times.Never);
    }
    [Fact]
    public async Task Handle_WhenAttributeExists_ShouldDeleteAndSave()
    {
        var attributeId = Guid.NewGuid();
        var command = new DeleteProductAttributeCommand(attributeId);

        var existingAttribute = new ProductAttribute("Color");

        _unitOfWorkMock.Setup(u => u.ProductAttributes.GetByIdAsync(attributeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAttribute);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _unitOfWorkMock.Verify(u => u.ProductAttributes.Delete(existingAttribute), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}