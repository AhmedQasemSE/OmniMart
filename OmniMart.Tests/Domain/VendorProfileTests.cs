using FluentAssertions;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Events;
using System;
using System.Linq;
using Xunit;

namespace OmniMart.Tests.Domain.Entities;

public class VendorProfileTests
{
    #region Helper Methods

    private VendorProfile CreateTestVendor(
        Guid? userId = null,
        string storeName = "Tech Store",
        string crNumber = "CR-12345",
        decimal commissionRate = 10m,
        string vendorNumber = "VND-001")
    {
        return new VendorProfile(
            userId ?? Guid.NewGuid(), 
            storeName,
            crNumber,
            commissionRate,
            vendorNumber
        );
    }

    #endregion

    #region Part 1: Constructor & Initialization Tests

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenUserIdIsEmpty()
    {
        Action act = () => CreateTestVendor(userId: Guid.Empty);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*valid User*")
           .And.ParamName.Should().Be("userId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrowArgumentException_WhenStoreNameIsInvalid(string? invalidStoreName)
    {
        
        Action act = () => CreateTestVendor(storeName: invalidStoreName!);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Store name cannot be empty*")
           .And.ParamName.Should().Be("storeName");
    }

    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly_WhenValidArgumentsProvided()
    {
        var userId = Guid.NewGuid();
        var storeName = "Super Tech";
        var crNumber = "CR-998877";
        var commissionRate = 15.5m;
        var vendorNumber = "VND-2026";

        var vendor = CreateTestVendor(userId, storeName, crNumber, commissionRate, vendorNumber);

        vendor.Id.Should().NotBeEmpty();
        vendor.UserId.Should().Be(userId);
        vendor.StoreName.Should().Be(storeName);
        vendor.CommercialRegisterNumber.Should().Be(crNumber);
        vendor.CommissionRate.Should().Be(commissionRate);
        vendor.VendorNumber.Should().Be(vendorNumber);
        vendor.CurrentBalance.Should().Be(0); 
        vendor.IsApproved.Should().BeFalse(); 
    }

    [Fact]
    public void Constructor_ShouldAddVendorRegisteredEvent_WhenCreatedSuccessfully()
    {
        var vendor = CreateTestVendor(); 

        var domainEvents = vendor.DomainEvents.ToList();
        domainEvents.Should().HaveCount(1);

        var registeredEvent = domainEvents.First().As<VendorRegisteredEvent>();
        registeredEvent.Should().NotBeNull();
        registeredEvent.VendorId.Should().Be(vendor.Id);
    }

    #endregion

    #region Part 2: Financial Operations & Account Status Tests

    [Fact]
    public void ApproveAccount_ShouldSetIsApprovedToTrue_WhenNotAlreadyApproved()
    {
        var vendor = CreateTestVendor(); 

        vendor.ApproveAccount();

        vendor.IsApproved.Should().BeTrue();
    }

    [Fact]
    public void ApproveAccount_ShouldThrowInvalidOperationException_WhenAlreadyApproved()
    {
        var vendor = CreateTestVendor();
        vendor.ApproveAccount();

        Action act = () => vendor.ApproveAccount(); 

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("This vendor is already approved.");
    }

    [Fact]
    public void ReceiveSaleRevenue_ShouldIncreaseBalanceCorrectly_AfterDeductingPlatformCommission()
    {
        var vendor = CreateTestVendor(commissionRate: 15m);

        vendor.ReceiveSaleRevenue(1000m); 

        vendor.CurrentBalance.Should().Be(850m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void ReceiveSaleRevenue_ShouldThrowArgumentException_WhenAmountIsInvalid(decimal invalidAmount)
    {
        var vendor = CreateTestVendor();

        Action act = () => vendor.ReceiveSaleRevenue(invalidAmount);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*positive value*");
    }

    [Fact]
    public void DeductRefundAmount_ShouldDecreaseBalanceCorrectly_AfterCalculatingPlatformCommission()
    {
        var vendor = CreateTestVendor(commissionRate: 10m);
        vendor.ReceiveSaleRevenue(1000m); 

        vendor.DeductRefundAmount(500m);

        vendor.CurrentBalance.Should().Be(450m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void DeductRefundAmount_ShouldThrowArgumentException_WhenAmountIsInvalid(decimal invalidAmount)
    {
        var vendor = CreateTestVendor();

        Action act = () => vendor.DeductRefundAmount(invalidAmount);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*positive value*");
    }

    [Fact]
    public void UpdateCommissionRate_ShouldUpdateSuccessfully_WhenRateIsValid()
    {
        var vendor = CreateTestVendor(commissionRate: 10m);

        vendor.UpdateCommissionRate(25m); 

        vendor.CommissionRate.Should().Be(25m);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void UpdateCommissionRate_ShouldThrowArgumentException_WhenRateIsOutOfRange(decimal invalidRate)
    {
        var vendor = CreateTestVendor();

        Action act = () => vendor.UpdateCommissionRate(invalidRate);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*between 0 and 100*");
    }

    #endregion

    #region Part 3: Profile Update & Reapproval Tests

    [Fact]
    public void UpdateProfile_ShouldUpdateDetails_WhenValidDataProvided()
    {
        var vendor = CreateTestVendor(storeName: "Old Store", crNumber: "CR-OLD");

        vendor.UpdateProfile("New Store Name", "CR-NEW");

        vendor.StoreName.Should().Be("New Store Name");
        vendor.CommercialRegisterNumber.Should().Be("CR-NEW");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateProfile_ShouldThrowArgumentException_WhenStoreNameIsInvalid(string? invalidName)
    {
        var vendor = CreateTestVendor();

        Action act = () => vendor.UpdateProfile(invalidName!, "CR-12345");

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Store name cannot be empty*")
           .And.ParamName.Should().Be("storeName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateProfile_ShouldThrowArgumentException_WhenCommercialRegisterNumberIsInvalid(string? invalidCrn)
    {
        var vendor = CreateTestVendor();

        Action act = () => vendor.UpdateProfile("Valid Store", invalidCrn!);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Commercial register number cannot be empty*")
           .And.ParamName.Should().Be("commercialRegisterNumber");
    }

    [Fact]
    public void UpdateProfile_ShouldRevokeApprovalAndTriggerEvent_WhenCRNChangesAndVendorIsApproved()
    {
        var vendor = CreateTestVendor(crNumber: "CR-OLD");
        vendor.ApproveAccount(); 

        vendor.UpdateProfile("Same Store", "CR-NEW");

        vendor.IsApproved.Should().BeFalse(); 

        var reapprovalEvent = vendor.DomainEvents.OfType<VendorRequiresReapprovalEvent>().SingleOrDefault();
        reapprovalEvent.Should().NotBeNull();
        reapprovalEvent!.OldCommercialRegisterNumber.Should().Be("CR-OLD");
        reapprovalEvent.NewCommercialRegisterNumber.Should().Be("CR-NEW");
    }

    [Fact]
    public void UpdateProfile_ShouldNotRevokeApprovalOrTriggerEvent_WhenCRNIsUnchanged()
    {
        var vendor = CreateTestVendor(crNumber: "CR-SAME");
        vendor.ApproveAccount();

        vendor.UpdateProfile("New Store Name", "CR-SAME");

        vendor.IsApproved.Should().BeTrue(); 

        var reapprovalEvent = vendor.DomainEvents.OfType<VendorRequiresReapprovalEvent>().SingleOrDefault();
        reapprovalEvent.Should().BeNull();
    }

    #endregion
}