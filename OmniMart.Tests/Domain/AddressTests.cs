using FluentAssertions;
using OmniMart.Domain.Common;
using System;
using Xunit;

namespace OmniMart.Tests.Domain.Common;

public class AddressTests
{
    private Address CreateTestAddress(
        string city = "Istanbul",
        string street = "Istiklal",
        string zipCode = "34000",
        string phone = "0555555555")
    {
        return new Address(city, street, zipCode, phone);
    }

    [Fact]
    public void Constructor_ShouldCreateAddress_WhenDataIsValid()
    {
        var address = CreateTestAddress();

        address.Should().NotBeNull();
        address.City.Should().Be("Istanbul");
        address.Street.Should().Be("Istiklal");
        address.ZipCode.Should().Be("34000");
        address.PhoneNumber.Should().Be("0555555555");
    }

    [Theory]
    [InlineData("", "Istiklal")]
    [InlineData(" ", "Istiklal")]
    [InlineData(null, "Istiklal")]
    [InlineData("Istanbul", "")]
    [InlineData("Istanbul", null)]
    public void Constructor_ShouldThrowException_WhenRequiredFieldsAreEmpty(string? invalidCity, string? invalidStreet)
    {
        Action act = () => new Address(invalidCity!, invalidStreet!, "34000", "0555555555");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RecordEquality_ShouldBeTrue_WhenValuesAreIdentical()
    {
        var address1 = CreateTestAddress();
        var address2 = CreateTestAddress();

        (address1 == address2).Should().BeTrue();
        address1.Should().BeEquivalentTo(address2);
    }
}