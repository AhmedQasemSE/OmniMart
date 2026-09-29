using FluentAssertions;
using OmniMart.Domain.Entities;
using System;
using Xunit;

namespace OmniMart.Tests.Domain.Entities;

public class CustomerAddressTests
{
    private CustomerAddress CreateTestCustomerAddress(Guid? customerId = null)
    {
        return new CustomerAddress(
            customerId ?? Guid.NewGuid(),
            "Home",
            "Istanbul",
            "Istiklal",
            "34000",
            "0555555555"
        );
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldCreateCustomerAddress_WhenValid()
    {
        var address = CreateTestCustomerAddress();

        address.Should().NotBeNull();
        address.Id.Should().NotBeEmpty();
        address.Title.Should().Be("Home");
        address.City.Should().Be("Istanbul");
        address.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenCustomerIdIsEmpty()
    {
        Action act = () => CreateTestCustomerAddress(Guid.Empty);

        act.Should().Throw<ArgumentException>().WithMessage("*Customer ID is required.*");
    }

    #endregion

    #region Methods Tests

    [Fact]
    public void MarkAsDeleted_ShouldSetIsDeletedToTrue()
    {
        var address = CreateTestCustomerAddress();

        address.MarkAsDeleted();

        address.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void UpdateDetails_ShouldUpdateProperties_WhenDataIsValid()
    {
        var address = CreateTestCustomerAddress();

        address.UpdateDetails("Work", "Ankara", "Ataturk", "06000", "0533333333");

        address.Title.Should().Be("Work");
        address.City.Should().Be("Ankara");
        address.Street.Should().Be("Ataturk");
        address.ZipCode.Should().Be("06000");
        address.PhoneNumber.Should().Be("0533333333");
    }

    [Theory]
    [InlineData("", "Ankara", "Ataturk")]
    [InlineData("Work", "", "Ataturk")]
    [InlineData("Work", "Ankara", "")]
    [InlineData(null, "Ankara", "Ataturk")]
    public void UpdateDetails_ShouldThrowException_WhenRequiredFieldsAreEmpty(string? invalidTitle, string? invalidCity, string? invalidStreet)
    {
        var address = CreateTestCustomerAddress();

        Action act = () => address.UpdateDetails(invalidTitle!, invalidCity!, invalidStreet!, "06000", "0533333333");

        act.Should().Throw<ArgumentException>();
    }

    #endregion
}