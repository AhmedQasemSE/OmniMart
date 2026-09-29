namespace OmniMart.Domain.Entities;

public class CustomerAddress
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }

    public string Title { get; private set; } 
    public string City { get; private set; }
    public string Street { get; private set; }
    public string ZipCode { get; private set; }
    public string PhoneNumber { get; private set; }
    public bool IsDeleted { get; private set; }
    public virtual CustomerProfile? Customer { get; private set; }

    public CustomerAddress(Guid customerId, string title, string city, string street, string zipCode, string phoneNumber)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("Customer ID is required.");

        Id = Guid.NewGuid();
        CustomerId = customerId;
        Title = title;
        City = city;
        Street = street;
        ZipCode = zipCode;
        PhoneNumber = phoneNumber;
    }
#pragma warning disable CS8618
    protected CustomerAddress() { }
#pragma warning disable CS8618
    public void MarkAsDeleted()
    {
        IsDeleted = true;
    }
    public void UpdateDetails(string title, string city, string street, string zipCode, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.");
        if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("City is required.");
        if (string.IsNullOrWhiteSpace(street)) throw new ArgumentException("Street is required.");

        Title = title;
        City = city;
        Street = street;
        ZipCode = zipCode;
        PhoneNumber = phoneNumber;
    }
}