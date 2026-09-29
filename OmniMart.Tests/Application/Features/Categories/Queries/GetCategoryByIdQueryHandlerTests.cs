using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Categories.Queries.GetCategoryById;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Entities;
using OmniMart.Tests.Builders;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Categories.Queries;

public class GetCategoryByIdQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly GetCategoryByIdQueryHandler _handler;
    private readonly Category _defaultCategory;

    public GetCategoryByIdQueryHandlerTests()
    {
        _handler = new GetCategoryByIdQueryHandler(_contextMock.Object);
        _defaultCategory = new CategoryBuilder().Build();
    }

    #region Helper Methods

    private void SetupCategories(params Category[] categories)
    {
        var mockDbSet = categories.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.Categories).Returns(mockDbSet.Object);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenCategoryDoesNotExist()
    {
        SetupCategories(); 

        var query = new GetCategoryByIdQuery(Guid.NewGuid());
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public async Task Handle_ShouldReturnCategoryDetailDto_WhenCategoryExists()
    {
        SetupCategories(_defaultCategory);

        var query = new GetCategoryByIdQuery(_defaultCategory.Id);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(_defaultCategory.Id);
        result.Value.Name.Should().Be(_defaultCategory.Name);
    }

    #endregion
}