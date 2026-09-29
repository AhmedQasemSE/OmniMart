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

public class DeleteCategoryCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly Mock<ILogger<DeleteCategoryCommandHandler>> _loggerMock = new();

    private readonly DeleteCategoryCommandHandler _handler;
    private readonly Category _defaultCategory;

    public DeleteCategoryCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Categories).Returns(_categoryRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepoMock.Object);

        _handler = new DeleteCategoryCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _cacheMock.Object
        );
        _defaultCategory = new CategoryBuilder().Build();
    }

    #region Helper Methods
    private void SetupDefaultSuccessBehavior()
    {
        _categoryRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                         .ReturnsAsync(_defaultCategory);

        _categoryRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _productRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);


    }


    #endregion

    #region Tests

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        SetupDefaultSuccessBehavior();
        _categoryRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                 .ReturnsAsync((Category)null!);

        var command = new DeleteCategoryCommand(Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound); 

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenCategoryHasSubCategories()
    {
        SetupDefaultSuccessBehavior();
        _categoryRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(true);


        var command = new DeleteCategoryCommand(_defaultCategory.Id);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("contains sub-categories"); 

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenCategoryHasLinkedProducts()
    { 
        SetupDefaultSuccessBehavior();
        _productRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(true);

        var command = new DeleteCategoryCommand(_defaultCategory.Id);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict);
        result.ErrorMessage.Should().Contain("products linked"); 

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSoftDeleteCategory_AndClearCache_WhenAllConditionsAreMet()
    {
        SetupDefaultSuccessBehavior();

        var command = new DeleteCategoryCommand(_defaultCategory.Id);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _defaultCategory.IsDeleted.Should().BeTrue(); 

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(CacheKeys.AllCategories, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}