using FluentAssertions;
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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Categories.Commands;

public class CreateCategoryCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly Mock<ILogger<CreateCategoryCommandHandler>> _loggerMock = new();

    private readonly CreateCategoryCommandHandler _handler;
    private readonly Category _Category;

    public CreateCategoryCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Categories).Returns(_categoryRepoMock.Object);

        _handler = new CreateCategoryCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _cacheMock.Object
        );
        _Category = new CategoryBuilder().Build();
    }

    #region Helper Methods

    private void SetupCreateCategorySuccessBehavior()
    {
        _categoryRepoMock.Setup(r => r.IsCategoryNameExistsUnderParentAsync(
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _categoryRepoMock.Setup(r => r.GetAsync(
            It.IsAny<Expression<Func<Category, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Expression<Func<Category, object>>[]>()))
            .ReturnsAsync(_Category);
    }


    #endregion

    #region Tests

    [Fact]
    public async Task Handle_ShouldCreateCategory_AndClearCache_WhenDataIsValid()
    {
        var command = new CreateCategoryCommand("Electronics", "Tech items", null);
        SetupCreateCategorySuccessBehavior();

       var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _categoryRepoMock.Verify(r => r.AddAsync(It.Is<Category>(c => c.Name == command.Name), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(CacheKeys.AllCategories, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCreateSubCategory_WhenDepthIsValid()
    {
        var command = new CreateCategoryCommand("Laptops", "Laptops desc", _Category.Id);

        SetupCreateCategorySuccessBehavior();

         var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenCategoryNameAlreadyExists()
    {
        var command = new CreateCategoryCommand("Electronics", "Tech items", null);

        _categoryRepoMock.Setup(r => r.IsCategoryNameExistsUnderParentAsync(
    It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(true);


        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);

        _categoryRepoMock.Verify(r => r.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenParentCategoryDoesNotExist()
    {
        var command = new CreateCategoryCommand("Electronics", "Tech items", Guid.NewGuid());

        SetupCreateCategorySuccessBehavior();
        _categoryRepoMock.Setup(r => r.GetAsync(
    It.IsAny<Expression<Func<Category, bool>>>(),
    It.IsAny<CancellationToken>(),
    It.IsAny<Expression<Func<Category, object>>[]>()))
    .ReturnsAsync((Category)null!);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenMaxDepthOf3LevelsIsReached()
    {
        var command = new CreateCategoryCommand("IPhone 15", "desc", Guid.NewGuid());
        SetupCreateCategorySuccessBehavior();

        var greatGrandpaId = Guid.NewGuid();

        var grandpaCategory = new CategoryBuilder()
            .WithId(Guid.NewGuid())
            .WithParent(greatGrandpaId)
            .Build();

        var fatherCategory = new CategoryBuilder()
            .WithId(Guid.NewGuid())
            .WithParent(grandpaCategory.Id)
            .WithParentCategoryEntity(grandpaCategory)
            .Build();
        _categoryRepoMock.Setup(r => r.GetAsync(
        It.IsAny<Expression<Func<Category, bool>>>(),
        It.IsAny<CancellationToken>(),
        It.IsAny<Expression<Func<Category, object>>[]>()))
         .ReturnsAsync(fatherCategory);


        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Failure);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}