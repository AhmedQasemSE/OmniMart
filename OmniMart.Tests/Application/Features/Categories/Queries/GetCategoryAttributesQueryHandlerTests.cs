using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Categories.Queries;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Entities;
using OmniMart.Tests.Builders;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniMart.Tests.Application.Features.Categories.Queries;

public class GetCategoryAttributesQueryHandlerTests
{
    private readonly Mock<IAppDbContext> _contextMock = new();
    private readonly Mock<ILogger<GetCategoryAttributesQueryHandler>> _loggerMock = new();

    private readonly GetCategoryAttributesQueryHandler _handler;
    private readonly Category _defaultCategory;
    public GetCategoryAttributesQueryHandlerTests()
    {
        _handler = new GetCategoryAttributesQueryHandler(
            _contextMock.Object,
            _loggerMock.Object
        );
        _defaultCategory = new CategoryBuilder().Build();

    }

    #region Helper Methods

    private void SetupCategoryAttributes(params CategoryAttribute[] attributes)
    {
        var mockDbSet = attributes.BuildMockDbSet();
        _contextMock.Setup(c => c.CategoryAttributes).Returns(mockDbSet.Object);
    }

    #endregion

    #region Tests

    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenCategoryHasNoAttributes()
    {
        SetupCategoryAttributes();

        var query = new GetCategoryAttributesQuery(Guid.NewGuid());
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedAttributes_WhenCategoryHasAttributes()
    {
       

        var productAttribute = new ProductAttributeBuilder()
            .WithName("Color")
            .Build();

        var categoryAttribute = new CategoryAttributeBuilder()
            .WithCategory(_defaultCategory.Id)
            .WithProductAttributeId(productAttribute.Id)
            .WithIsRequired(true)
            .WithProductAttributeEntity(productAttribute) !
            .Build();

        SetupCategoryAttributes(categoryAttribute);

        var query = new GetCategoryAttributesQuery(_defaultCategory.Id);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);

        var returnedItem = result.Value!.First();
        returnedItem.AttributeId.Should().Be(productAttribute.Id);
        returnedItem.Name.Should().Be("Color");
        returnedItem.IsRequired.Should().BeTrue();
    }

    #endregion
}