using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Attributes.Queries;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Entities;
using OmniMart.Tests.Builders;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Attributes.Queries;

public class GetAllProductAttributesQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly Mock<ILogger<GetAllProductAttributesQueryHandler>> _loggerMock = new();
    private readonly GetAllProductAttributesQueryHandler _handler;

    public GetAllProductAttributesQueryHandlerTests()
    {
        _handler = new GetAllProductAttributesQueryHandler(_contextMock.Object, _loggerMock.Object);
    }

    #region Helper Methods

    private void SetupData(params ProductAttribute[] attributes)
    {
        var mockDbSet = attributes.ToList().BuildMockDbSet();
        _contextMock.Setup(c => c.ProductAttributes).Returns(mockDbSet.Object);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_WhenDatabaseIsEmpty_ShouldReturnEmptyPaginatedResult()
    {
        SetupData(); 

        var query = new GetAllProductAttributesQuery(1, 10);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Data.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.CurrentPage.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenDataExists_ShouldReturnSortedAndPaginatedResult()
    {
        var attr1 = new ProductAttributeBuilder().WithName("Weight").Build();
        var attr2 = new ProductAttributeBuilder().WithName("Color").Build();
        var attr3 = new ProductAttributeBuilder().WithName("Size").Build();

        SetupData(attr1, attr2, attr3);

        var query = new GetAllProductAttributesQuery(PageNumber: 1, PageSize: 2);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(3);
        result.Value.Data.Should().HaveCount(2);

        result.Value.Data[0].Name.Should().Be("Color");
        result.Value.Data[1].Name.Should().Be("Size");
    }

    #endregion
}