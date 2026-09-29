using OmniMart.Domain.Entities;
using System;

namespace OmniMart.Tests.Builders;

public class VendorProfileBuilder
{
    private Guid? _id = null;
    private Guid _userId = Guid.NewGuid();
    private string _storeName = "Default Store";
    private string _commercialRegisterNumber = "CR-123456";
    private decimal _commissionRate = 10m;
    private string _vendorNumber = "VND-0001";

    private User? _customUser = null;
    private bool _isApproved = false;
    private decimal _currentBalance = 0m; 

    public VendorProfileBuilder WithId(Guid id) { _id = id; return this; }
    public VendorProfileBuilder WithUserId(Guid userId) { _userId = userId; return this; }
    public VendorProfileBuilder WithStoreName(string storeName) { _storeName = storeName; return this; }
    public VendorProfileBuilder WithCommercialRegisterNumber(string crn) { _commercialRegisterNumber = crn; return this; }
    public VendorProfileBuilder WithCommissionRate(decimal rate) { _commissionRate = rate; return this; }
    public VendorProfileBuilder WithVendorNumber(string vendorNumber) { _vendorNumber = vendorNumber; return this; }

    public VendorProfileBuilder AsApproved() { _isApproved = true; return this; }

    public VendorProfileBuilder WithCurrentBalance(decimal balance)
    {
        _currentBalance = balance;
        return this;
    }

    public VendorProfileBuilder WithUser(User user)
    {
        _customUser = user;
        _userId = user.Id;
        return this;
    }

    public VendorProfile Build()
    {
        var profile = new VendorProfile(
            _userId,
            _storeName,
            _commercialRegisterNumber,
            _commissionRate,
            _vendorNumber);

        if (_id.HasValue)
        {
            typeof(VendorProfile).GetProperty("Id")?.SetValue(profile, _id.Value);
        }

        if (_customUser != null)
        {
            typeof(VendorProfile).GetProperty("User")?.SetValue(profile, _customUser);
        }

        if (_isApproved)
        {
            profile.ApproveAccount();
        }

        if (_currentBalance > 0)
        {
            var originalRate = _commissionRate;
            profile.UpdateCommissionRate(0m);
            profile.ReceiveSaleRevenue(_currentBalance);
            profile.UpdateCommissionRate(originalRate);
        }

        return profile;
    }
}