using FluentAssertions;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Events;
using System;
using System.Linq;
using Xunit;

namespace OmniMart.Tests.Domain.Entities;

public class CustomerProfileTests
{
    private CustomerProfile CreateTestCustomerProfile(Guid? userId = null)
    {
        return new CustomerProfile(userId ?? Guid.NewGuid());
    }

    #region 1. Constructor & Domain Events Tests

    [Fact]
    public void Constructor_ShouldCreateProfileAndRaiseEvent_WhenUserIdIsValid()
    {
        var userId = Guid.NewGuid();

        var profile = CreateTestCustomerProfile(userId);

        profile.Should().NotBeNull();
        profile.Id.Should().NotBeEmpty();
        profile.UserId.Should().Be(userId);
        profile.LoyaltyPoints.Should().Be(0);
        profile.TotalSpent.Should().Be(0);

        profile.DomainEvents.Should().ContainSingle(e => e is CustomerRegisteredEvent);
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenUserIdIsEmpty()
    {
        Action act = () => CreateTestCustomerProfile(Guid.Empty);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Customer Profile must be linked to a valid User*");
    }

    #endregion

    #region 2. Purchases, Loyalty Points & Payment Tests

    [Fact]
    public void RecordPurchase_ShouldIncreaseTotalSpentAndCalculateLoyaltyPoints()
    {
        var profile = CreateTestCustomerProfile();

        profile.RecordPurchase(150.50m);

        profile.RecordPurchase(50m);

        profile.TotalSpent.Should().Be(200.50m);
        profile.LoyaltyPoints.Should().Be(20); 
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void RecordPurchase_ShouldThrowException_WhenAmountIsZeroOrNegative(decimal invalidAmount)
    {
        var profile = CreateTestCustomerProfile();

        Action act = () => profile.RecordPurchase(invalidAmount);

        act.Should().Throw<ArgumentException>().WithMessage("*must be a positive value*");
    }

    [Fact]
    public void RedeemLoyaltyPoints_ShouldDeductPoints_WhenPointsAreAvailable()
    {
        var profile = CreateTestCustomerProfile();
        profile.RecordPurchase(500m); 

        profile.RedeemLoyaltyPoints(20);

        profile.LoyaltyPoints.Should().Be(30);
    }

    [Fact]
    public void RedeemLoyaltyPoints_ShouldThrowException_WhenNotEnoughPoints()
    {
        var profile = CreateTestCustomerProfile();
        profile.RecordPurchase(100m); 

        Action act = () => profile.RedeemLoyaltyPoints(20);

        act.Should().Throw<InvalidOperationException>().WithMessage("Not enough loyalty points to redeem.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void RedeemLoyaltyPoints_ShouldThrowException_WhenPointsAreZeroOrNegative(int invalidPoints)
    {
        var profile = CreateTestCustomerProfile();

        Action act = () => profile.RedeemLoyaltyPoints(invalidPoints);

        act.Should().Throw<ArgumentException>().WithMessage("*must be a positive value*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void LinkPaymentMethod_ShouldThrowException_WhenTokenIsInvalid(string? invalidToken)
    {
        var profile = CreateTestCustomerProfile();

        Action act = () => profile.LinkPaymentMethod(invalidToken!);

        act.Should().Throw<ArgumentException>().WithMessage("*Payment token cannot be empty*");
    }
    [Fact]
    public void LinkPaymentMethod_ShouldSetExternalPaymentId()
    {
        var profile = CreateTestCustomerProfile();
        var token = "stripe_cust_123";

        profile.LinkPaymentMethod(token);

        profile.ExternalPaymentCustomerId.Should().Be(token);
    }

    [Fact]
    public void DeductPointsForRefund_ShouldDeductCalculatedPoints()
    {
        var profile = CreateTestCustomerProfile();
        profile.RecordPurchase(200m); 

        profile.DeductPointsForRefund(50m);

        profile.LoyaltyPoints.Should().Be(15);
    }

   

    #endregion

    #region 3. Address Management Tests

    [Fact]
    public void AddAddress_ShouldAddAddressToCollectionAndReturnId()
    {
        var profile = CreateTestCustomerProfile();

        var addressId = profile.AddAddress("Home", "Istanbul", "Istiklal", "34000", "0555555555");

        addressId.Should().NotBeEmpty();
        profile.Addresses.Should().ContainSingle(a => a.Id == addressId);
    }

    [Fact]
    public void AddAddress_ShouldThrowException_WhenAddingMoreThan10Addresses()
    {
        var profile = CreateTestCustomerProfile();

        profile.AddAddress("Title 1", "City", "Street 1", "Zip", "Phone");
        profile.AddAddress("Title 2", "City", "Street 2", "Zip", "Phone");
        profile.AddAddress("Title 3", "City", "Street 3", "Zip", "Phone");
        profile.AddAddress("Title 4", "City", "Street 4", "Zip", "Phone");
        profile.AddAddress("Title 5", "City", "Street 5", "Zip", "Phone");
        profile.AddAddress("Title 6", "City", "Street 6", "Zip", "Phone");
        profile.AddAddress("Title 7", "City", "Street 7", "Zip", "Phone");
        profile.AddAddress("Title 8", "City", "Street 8", "Zip", "Phone");
        profile.AddAddress("Title 9", "City", "Street 9", "Zip", "Phone");
        profile.AddAddress("Title 10", "City", "Street 10", "Zip", "Phone");

        Action act = () => profile.AddAddress("Work", "Istanbul", "Ataturk", "34111", "12345");

        act.Should().Throw<InvalidOperationException>().WithMessage("Cannot add more than 10 addresses.");
    }
    [Fact]
    public void AddAddress_ShouldThrowException_WhenTitleOrLocationIsDuplicate()
    {
        var profile = CreateTestCustomerProfile();
        profile.AddAddress("Home", "Istanbul", "Istiklal", "34000", "0555555555");

        Action duplicateTitle = () => profile.AddAddress("Home", "Ankara", "Test", "06000", "111");

        Action duplicateLocation = () => profile.AddAddress("Work", "Istanbul", "Istiklal", "34000", "222");

        duplicateTitle.Should().Throw<InvalidOperationException>().WithMessage("*already exists*");
        duplicateLocation.Should().Throw<InvalidOperationException>().WithMessage("*already exists*");
    }

    [Fact]
    public void RemoveAddress_ShouldMarkAddressAsDeleted()
    {
        var profile = CreateTestCustomerProfile();
        var addressId = profile.AddAddress("Home", "Istanbul", "Istiklal", "34000", "0555555555");

        profile.RemoveAddress(addressId);

        var address = profile.Addresses.First();
        address.IsDeleted.Should().BeTrue();
    }
    [Fact]
    public void RemoveAddress_ShouldThrowException_WhenAddressNotFound()
    {
        var profile = CreateTestCustomerProfile();

        Action act = () => profile.RemoveAddress(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>().WithMessage("Address not found.");
    }
    [Fact]
    public void UpdateAddress_ShouldUpdateDetails_WhenAddressExistsAndNotDuplicate()
    {
        var profile = CreateTestCustomerProfile();
        var addressId = profile.AddAddress("Home", "Istanbul", "Istiklal", "34000", "0555555555");

        profile.UpdateAddress(addressId, "Home Updated", "Ankara", "Ataturk", "06000", "0533333333");

        var address = profile.Addresses.First();
        address.Title.Should().Be("Home Updated");
        address.City.Should().Be("Ankara");
    }
   

    [Fact]
    public void UpdateAddress_ShouldThrowException_WhenAddressNotFound()
    {
        var profile = CreateTestCustomerProfile();

        Action act = () => profile.UpdateAddress(Guid.NewGuid(), "Title", "City", "Street", "Zip", "Phone");

        act.Should().Throw<InvalidOperationException>().WithMessage("Address not found.");
    }

    [Fact]
    public void UpdateAddress_ShouldThrowException_WhenUpdateCausesDuplicate()
    {
        var profile = CreateTestCustomerProfile();
        var addressId1 = profile.AddAddress("Home", "Istanbul", "Istiklal", "34000", "111");
        var addressId2 = profile.AddAddress("Work", "Ankara", "Ataturk", "06000", "222");

        Action act = () => profile.UpdateAddress(addressId2, "Home", "Istanbul", "Istiklal", "34000", "222");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Another address with this title or exact location already exists*");
    }
    #endregion
}