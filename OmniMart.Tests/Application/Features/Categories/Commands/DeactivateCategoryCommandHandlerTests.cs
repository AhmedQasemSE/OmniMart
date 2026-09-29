using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Categories.Commands;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using OmniMart.Domain.Entities;
using OmniMart.Tests.Builders;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Categories.Commands;

public class DeactivateCategoryCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IDistributedCache> _cacheMock = new();

    private readonly DeactivateCategoryCommandHandler _handler;
    private readonly Category _category;

    public DeactivateCategoryCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Categories).Returns(_categoryRepoMock.Object);

        _handler = new DeactivateCategoryCommandHandler(
            _unitOfWorkMock.Object,
            _cacheMock.Object
        );
        _category = new CategoryBuilder().WithIsActive(true).Build();
    }

    #region Helper Methods

    private void SetupDeactivateCategorySuccessBehavior()
    {
        _categoryRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(_category);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        _categoryRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync((Category)null!);
        var command = new DeactivateCategoryCommand(Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound); 

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationFailure_WhenCategoryIsAlreadyInactive()
    {
        var category = new CategoryBuilder().WithIsActive(false).Build();
        _categoryRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(category);

        var command = new DeactivateCategoryCommand(category.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation); 

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldDeactivateCategory_AndClearCache_WhenCategoryIsActive()
    {
        SetupDeactivateCategorySuccessBehavior();

        var command = new DeactivateCategoryCommand(_category.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _category.IsActive.Should().BeFalse();

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(CacheKeys.AllCategories, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}