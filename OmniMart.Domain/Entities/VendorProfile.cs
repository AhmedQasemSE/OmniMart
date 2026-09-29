using OmniMart.Domain.Common;
using OmniMart.Domain.Events;

namespace OmniMart.Domain.Entities;

public class VendorProfile:BaseEntity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string VendorNumber { get; private set; }
    public string StoreName { get; private set; }
    public string CommercialRegisterNumber { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public decimal CommissionRate { get; private set; }
    public bool IsApproved { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;
    public virtual User? User { get; private set; }

    public virtual ICollection<Product> Products { get; private set; } = new List<Product>();
    public VendorProfile(Guid userId, string storeName, string commercialRegisterNumber, decimal commissionRate, string vendorNumber)

    {
        if (userId == Guid.Empty)
            throw new ArgumentException("Vendor Profile must be linked to a valid User.", nameof(userId));

        if (string.IsNullOrWhiteSpace(storeName))
            throw new ArgumentException("Store name cannot be empty.", nameof(storeName));
        Id = Guid.NewGuid();
        UserId = userId;
        StoreName = storeName;
        CommercialRegisterNumber = commercialRegisterNumber;
        CurrentBalance = 0;
        VendorNumber = vendorNumber;
        CommissionRate = commissionRate;
        IsApproved = false;
        AddDomainEvent(new VendorRegisteredEvent(this.Id));
    }
#pragma warning disable CS8618
    protected VendorProfile() { }
#pragma warning restore CS8618

    public void ApproveAccount()
    {
        if (IsApproved)
            throw new InvalidOperationException("This vendor is already approved.");

        IsApproved = true;
    }

    public void ReceiveSaleRevenue(decimal saleAmount)
    {
        if (saleAmount <= 0)
            throw new ArgumentException("Sale amount must be a positive value.", nameof(saleAmount));
        decimal platformCommission = saleAmount * (CommissionRate / 100m);
        decimal vendorRevenue = saleAmount - platformCommission;
        CurrentBalance += vendorRevenue;
    }
    public void DeductRefundAmount(decimal saleAmount)
    {
        if (saleAmount <= 0)
            throw new ArgumentException("Refund amount must be a positive value.", nameof(saleAmount));

        decimal platformCommission = saleAmount * (CommissionRate / 100m);
        decimal vendorRevenueToDeduct = saleAmount - platformCommission;

        CurrentBalance -= vendorRevenueToDeduct;
    }
    public void UpdateCommissionRate(decimal newRate)
    {
        if (newRate < 0 || newRate > 100)
            throw new ArgumentException("Commission rate must be between 0 and 100.", nameof(newRate));

        CommissionRate = newRate;
    }
    public void UpdateProfile(string storeName, string commercialRegisterNumber)
    {
        if (string.IsNullOrWhiteSpace(storeName))
            throw new ArgumentException("Store name cannot be empty.", nameof(storeName));

        if (string.IsNullOrWhiteSpace(commercialRegisterNumber))
            throw new ArgumentException("Commercial register number cannot be empty.", nameof(commercialRegisterNumber));

        StoreName = storeName;

        if (CommercialRegisterNumber != commercialRegisterNumber)
        {
            string oldCommercialRegisterNumber = CommercialRegisterNumber;

            CommercialRegisterNumber = commercialRegisterNumber;

            if (IsApproved)
            {
                IsApproved = false;
                AddDomainEvent(new VendorRequiresReapprovalEvent(this.Id, oldCommercialRegisterNumber, commercialRegisterNumber));
            }
        }
    }
}
