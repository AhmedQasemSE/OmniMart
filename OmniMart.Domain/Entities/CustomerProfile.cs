using OmniMart.Domain.Common;
using OmniMart.Domain.Events;

namespace OmniMart.Domain.Entities;

public class CustomerProfile:BaseEntity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string? ExternalPaymentCustomerId { get; private set; }
    public int LoyaltyPoints { get; private set; }
    public decimal TotalSpent { get; private set; }
    private readonly List<CustomerAddress> _addresses = new();
    public virtual IReadOnlyCollection<CustomerAddress> Addresses => _addresses.AsReadOnly();
    public virtual ICollection<Order> Orders { get; private set; } = new List<Order>(); 
    public virtual User? User { get; private set; }
    public virtual Cart? Cart { get; private set; }
    public CustomerProfile(Guid userId)
    {
        Id = Guid.NewGuid();
        if (userId == Guid.Empty)
            throw new ArgumentException("Customer Profile must be linked to a valid User.", nameof(userId));
        UserId = userId;
        LoyaltyPoints = 0;
        TotalSpent = 0;
        AddDomainEvent(new CustomerRegisteredEvent(this.Id));
    }
#pragma warning disable CS8618
    protected CustomerProfile() { }
#pragma warning restore CS8618

    public void RecordPurchase(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Purchase amount must be a positive value.", nameof(amount));
        TotalSpent += amount;
        LoyaltyPoints += (int)(amount / 10);
    }

    public void LinkPaymentMethod(string paymentToken)
    {
        if (string.IsNullOrWhiteSpace(paymentToken))
            throw new ArgumentException("Payment token cannot be empty.", nameof(paymentToken));

        ExternalPaymentCustomerId = paymentToken;
    }
    public void RedeemLoyaltyPoints(int pointsToRedeem)
    {
        if (pointsToRedeem <= 0) throw new ArgumentException("Points to redeem must be a positive value.", nameof(pointsToRedeem));
        if (pointsToRedeem > LoyaltyPoints) throw new InvalidOperationException("Not enough loyalty points to redeem.");
        LoyaltyPoints -= pointsToRedeem;
    }
    public void DeductPointsForRefund(decimal amount)
    {
        int pointsToRemove = (int)(amount / 10);

        LoyaltyPoints -= pointsToRemove;
    }

    public Guid AddAddress(string title, string city, string street, string zipCode, string phoneNumber)
    {
        if (_addresses.Count >= 10)
            throw new InvalidOperationException("Cannot add more than 10 addresses.");

        bool isDuplicate = _addresses.Any(a =>
                    a.Title.Equals(title, StringComparison.OrdinalIgnoreCase)
                    ||
                    (
                        a.City.Equals(city, StringComparison.OrdinalIgnoreCase) &&
                        a.Street.Equals(street, StringComparison.OrdinalIgnoreCase) &&
                        a.ZipCode.Equals(zipCode, StringComparison.OrdinalIgnoreCase)
                    )
                );
        if (isDuplicate)
            throw new InvalidOperationException("An address with this title or exact location already exists.");

        var newAddress = new CustomerAddress(this.Id, title, city, street, zipCode, phoneNumber);
        _addresses.Add(newAddress);
        return newAddress.Id;
    }
    public void RemoveAddress(Guid addressId)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId);
        if (address == null)
            throw new InvalidOperationException("Address not found.");

        address.MarkAsDeleted();
    }
    public void UpdateAddress(Guid addressId, string title, string city, string street, string zipCode, string phoneNumber)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId && !a.IsDeleted);
        if (address == null)
            throw new InvalidOperationException("Address not found.");

        bool isDuplicate = _addresses.Any(a =>
            a.Id != addressId && !a.IsDeleted &&
            (a.Title.Equals(title, StringComparison.OrdinalIgnoreCase) ||
            (a.City.Equals(city, StringComparison.OrdinalIgnoreCase) &&
             a.Street.Equals(street, StringComparison.OrdinalIgnoreCase) &&
             a.ZipCode.Equals(zipCode, StringComparison.OrdinalIgnoreCase))));

        if (isDuplicate)
            throw new InvalidOperationException("Another address with this title or exact location already exists.");

        address.UpdateDetails(title, city, street, zipCode, phoneNumber);
    }
}
    