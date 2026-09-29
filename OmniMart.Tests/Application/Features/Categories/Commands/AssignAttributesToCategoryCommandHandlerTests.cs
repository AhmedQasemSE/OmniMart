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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Categories.Commands;

public class AssignAttributesToCategoryCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly Mock<ILogger<AssignAttributesToCategoryCommandHandler>> _loggerMock = new();
    private readonly Category _Category;
    private readonly AssignAttributesToCategoryCommandHandler _handler;

    public AssignAttributesToCategoryCommandHandlerTests()
    {
        _unitOfWorkMock.Setup(u => u.Categories).Returns(_categoryRepoMock.Object);

        _handler = new AssignAttributesToCategoryCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _cacheMock.Object
        );
        _Category = new CategoryBuilder().Build();
    }

    #region Helper Methods

    private void SetupAssignAttributesSuccessBehavior()
    {
        _categoryRepoMock.Setup(r => r.GetAsync(
            It.IsAny<Expression<Func<Category, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Expression<Func<Category, object>>[]>()))
            .ReturnsAsync(_Category);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        _categoryRepoMock.Setup(r => r.GetAsync(
           It.IsAny<Expression<Func<Category, bool>>>(),
           It.IsAny<CancellationToken>(),
           It.IsAny<Expression<Func<Category, object>>[]>()))
           .ReturnsAsync((Category)null!);

        var command = new AssignAttributesToCategoryCommand(Guid.NewGuid(), new List<CategoryAttributeRequestDto>(), new byte[] { 1, 2, 3 });
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenConcurrencyExceptionIsThrown()
    {
        SetupAssignAttributesSuccessBehavior();

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new DbUpdateConcurrencyException());

        var command = new AssignAttributesToCategoryCommand(_Category.Id, new List<CategoryAttributeRequestDto>(), new byte[] { 1, 2, 3 });
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Conflict); 
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationFailure_WhenDbUpdateExceptionIsThrown()
    {
        SetupAssignAttributesSuccessBehavior();

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new DbUpdateException());

        var command = new AssignAttributesToCategoryCommand(
            _Category.Id,
            new List<CategoryAttributeRequestDto> { new CategoryAttributeRequestDto(Guid.NewGuid(), true) },
            new byte[] { 1, 2, 3 });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.ErrorMessage.Should().Contain("do not exist in the system");
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationFailure_WhenInvalidOperationExceptionIsThrown()
    {
        SetupAssignAttributesSuccessBehavior();

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new InvalidOperationException("Domain layer error message"));

        var command = new AssignAttributesToCategoryCommand(
            _Category.Id,
            new List<CategoryAttributeRequestDto> { new CategoryAttributeRequestDto(Guid.NewGuid(), true) },
            new byte[] { 1, 2, 3 });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Validation);
        result.ErrorMessage.Should().Contain("Domain layer error message"); 
    }

    [Fact]
    public async Task Handle_ShouldUpdateAttributes_AndClearCache_WhenSuccessful()
    {
        var oldAttributeId = Guid.NewGuid();
        var keptAttributeId = Guid.NewGuid();
        var newAttributeId = Guid.NewGuid();

        _Category.AddAttribute(oldAttributeId, false);
        _Category.AddAttribute(keptAttributeId, false);

        SetupAssignAttributesSuccessBehavior();

        var requestedAttributes = new List<CategoryAttributeRequestDto>
        {
            new CategoryAttributeRequestDto(keptAttributeId, true),  
            new CategoryAttributeRequestDto(newAttributeId, false)  
        };

        var command = new AssignAttributesToCategoryCommand(_Category.Id, requestedAttributes, new byte[] { 1, 2, 3 });
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _Category.CategoryAttributes.Should().HaveCount(2);
        _Category.CategoryAttributes.Should().ContainSingle(a => a.ProductAttributeId == keptAttributeId && a.IsRequired == true);
        _Category.CategoryAttributes.Should().ContainSingle(a => a.ProductAttributeId == newAttributeId && a.IsRequired == false);
        _Category.CategoryAttributes.Should().NotContain(a => a.ProductAttributeId == oldAttributeId);

        _categoryRepoMock.Verify(r => r.SetOriginalRowVersion(_Category, command.RowVersion), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync($"Category_Attributes_{_Category.Id}", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}