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

public class ToggleCategoryStatusCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IDistributedCache> _cacheMock = new();

    private readonly ToggleCategoryStatusCommandHandler _handler;
    private readonly Category _defaultCategory;

    public ToggleCategoryStatusCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Categories).Returns(_categoryRepoMock.Object);

        _handler = new ToggleCategoryStatusCommandHandler(
            _unitOfWorkMock.Object,
            _cacheMock.Object
        );

        _defaultCategory = new CategoryBuilder().WithIsActive(true).Build();
    }

    #region Helper Methods

    private void SetupDefaultSuccessBehavior()
    {
        _categoryRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(_defaultCategory);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        _categoryRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync((Category)null!);

        var command = new ToggleCategoryStatusCommand(Guid.NewGuid());
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldToggleStatusToFalse_AndClearCache_WhenCategoryIsCurrentlyActive()
    {
        SetupDefaultSuccessBehavior();

        var command = new ToggleCategoryStatusCommand(_defaultCategory.Id);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _defaultCategory.IsActive.Should().BeFalse();

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(CacheKeys.AllCategories, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldToggleStatusToTrue_AndClearCache_WhenCategoryIsCurrentlyInactive()
    {
        var inactiveCategory = new CategoryBuilder().WithIsActive(false).Build();

        _categoryRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(inactiveCategory);

        var command = new ToggleCategoryStatusCommand(inactiveCategory.Id);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        inactiveCategory.IsActive.Should().BeTrue();

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(CacheKeys.AllCategories, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}