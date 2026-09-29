using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Categories.Queries.GetAllCategories;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Entities;
using OmniMart.Tests.Builders;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Categories.Queries;

public class GetAllCategoriesQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly Mock<ILogger<GetAllCategoriesQueryHandler>> _loggerMock = new();

    private readonly GetAllCategoriesQueryHandler _handler;

    public GetAllCategoriesQueryHandlerTests()
    {
        _handler = new GetAllCategoriesQueryHandler(_contextMock.Object, _loggerMock.Object);
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
    public async Task Handle_ShouldReturnEmptyList_WhenNoCategoriesExist()
    {
        SetupCategories();

        var query = new GetAllCategoriesQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnCategoryTree_WhenCategoriesExist()
    {
        var grandPa = new CategoryBuilder().WithName("Electronics").Build();

        var father = new CategoryBuilder().WithName("Laptops")
                                          .WithParent(grandPa.Id)
                                          .Build();

        var son = new CategoryBuilder().WithName("Gaming Laptops")
                                       .WithParent(father.Id)
                                       .Build();

        SetupCategories(grandPa, father, son);

        var query = new GetAllCategoriesQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);

        var rootDto = result.Value.First();
        rootDto.Name.Should().Be("Electronics");
        rootDto.SubCategories.Should().HaveCount(1);
    }

    #endregion
}