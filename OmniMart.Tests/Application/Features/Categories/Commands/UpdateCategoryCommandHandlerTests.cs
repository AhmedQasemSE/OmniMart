using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
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

public class UpdateCategoryCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly Mock<ILogger<UpdateCategoryCommandHandler>> _loggerMock = new();

    private readonly UpdateCategoryCommandHandler _handler;
    private readonly Category _defaultCategory;
    private readonly UpdateCategoryCommand _defaultCommand;

    public UpdateCategoryCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Categories).Returns(_categoryRepoMock.Object);

        _handler = new UpdateCategoryCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _cacheMock.Object
        );


        _defaultCategory = new CategoryBuilder()
            .WithName("OldName")
            .WithParent(null)
            .Build();

        _defaultCommand = new UpdateCategoryCommand(
            _defaultCategory.Id,
            Guid.NewGuid(),
            "NewName",
            "NewDescription",
            new byte[] { 1, 2, 3 }
        );
    }

    #region Helper Methods

    private void SetupDefaultSuccessBehavior()
    {
        _categoryRepoMock.Setup(c => c.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(_defaultCategory);

        _categoryRepoMock.Setup(c => c.IsCategoryNameExistsUnderParentAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(false);

        _categoryRepoMock.Setup(c => c.IsValidParentAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(true); 
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        SetupDefaultSuccessBehavior();

        _categoryRepoMock.Setup(c => c.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync((Category)null!);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound); 

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenNewNameAlreadyExists()
    {
        SetupDefaultSuccessBehavior();

        _categoryRepoMock.Setup(c => c.IsCategoryNameExistsUnderParentAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(true);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("already exists"); 

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenParentCategoryIsInvalid()
    {
        SetupDefaultSuccessBehavior();

        _categoryRepoMock.Setup(c => c.IsValidParentAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(false);

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Failure);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenDbUpdateConcurrencyExceptionIsThrown()
    {
        SetupDefaultSuccessBehavior();

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new DbUpdateConcurrencyException());

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("modified by another user"); 
    }

    [Fact]
    public async Task Handle_ShouldUpdateCategory_AndClearCache_WhenAllDataIsChangedAndValid()
    {
        SetupDefaultSuccessBehavior();

        var result = await _handler.Handle(_defaultCommand, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(_defaultCategory.Id);

        _categoryRepoMock.Verify(u => u.SetOriginalRowVersion(_defaultCategory, _defaultCommand.RowVersion), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(CacheKeys.AllCategories, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUpdateDescriptionOnly_WithoutValidationTrips_WhenNameAndParentAreUnchanged()
    {
        SetupDefaultSuccessBehavior();

        var commandNoValidation = new UpdateCategoryCommand(
            _defaultCategory.Id,
            _defaultCategory.ParentCategoryId,
            _defaultCategory.Name,
            "Only Description Changed",
            new byte[] { 1, 2, 3 }
        );

        var result = await _handler.Handle(commandNoValidation, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _categoryRepoMock.Verify(c => c.IsCategoryNameExistsUnderParentAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _categoryRepoMock.Verify(c => c.IsValidParentAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}