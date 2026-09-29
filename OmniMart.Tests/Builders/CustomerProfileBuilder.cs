using OmniMart.Domain.Entities;
using System;

namespace OmniMart.Tests.Builders;

public class CustomerProfileBuilder
{
    private Guid? _id = null;
    private Guid _userId = Guid.NewGuid();
    private User? _customUser = null;

    private int _loyaltyPoints = 0;
    private decimal _totalSpent = 0m;

    public CustomerProfileBuilder WithId(Guid id) { _id = id; return this; }

    public CustomerProfileBuilder WithUserId(Guid userId) { _userId = userId; return this; }

    public CustomerProfileBuilder WithLoyaltyPoints(int points) { _loyaltyPoints = points; return this; }

    public CustomerProfileBuilder WithTotalSpent(decimal total) { _totalSpent = total; return this; }

    public CustomerProfileBuilder WithUser(User user)
    {
        _customUser = user;
        _userId = user.Id;
        return this;
    }

    public CustomerProfile Build()
    {
        var profile = new CustomerProfile(_userId);

        if (_id.HasValue)
        {
            typeof(CustomerProfile).GetProperty("Id")?.SetValue(profile, _id.Value);
        }

        if (_loyaltyPoints > 0)
        {
            typeof(CustomerProfile).GetProperty("LoyaltyPoints")?.SetValue(profile, _loyaltyPoints);
        }

        if (_totalSpent > 0)
        {
            typeof(CustomerProfile).GetProperty("TotalSpent")?.SetValue(profile, _totalSpent);
        }

        if (_customUser != null)
        {
            typeof(CustomerProfile).GetProperty("User")?.SetValue(profile, _customUser);
            typeof(User).GetProperty("CustomerProfile")?.SetValue(_customUser, profile);
        }

        return profile;
    }
}