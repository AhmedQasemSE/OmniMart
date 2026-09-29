using OmniMart.Domain.Common;
using OmniMart.Domain.Enums;
using OmniMart.Domain.Events;
using System.Collections.Generic;

namespace OmniMart.Domain.Entities;

public class Order : BaseEntity
{
    public Guid Id {get; private set; }
    public Guid CustomerId {get; private set; }
    public Guid VendorId { get; private set; }
    public decimal TotalAmount {get; private set; }
    public DateTimeOffset OrderDate {get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public OrderStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;
    public Guid PaymentGroupId { get; private set; }
    public virtual PaymentGroup? PaymentGroup { get; private set; }
    public Address ShippingAddress { get; private set; }
    private readonly List<OrderItem> _orderItems = new();
    public virtual IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();
    public virtual CustomerProfile? Customer { get; private set; }
    public virtual VendorProfile? VendorProfile { get; private set; }
    public Order(Guid customerId, Guid vendorId, Guid paymentGroupId, PaymentMethod paymentMethod, Address shippingAddress) 
    {
        if (customerId == Guid.Empty) throw new ArgumentException("Customer ID cannot be empty.", nameof(customerId));
        Id = Guid.NewGuid();
        CustomerId = customerId;
        VendorId = vendorId;
        PaymentGroupId = paymentGroupId;
        TotalAmount = 0;
        OrderDate = DateTimeOffset.UtcNow;
        Status = OrderStatus.Pending;
        PaymentMethod = paymentMethod;
        ShippingAddress = shippingAddress;
    }

#pragma warning disable CS8618
    protected Order() { }
#pragma warning restore CS8618


    public void AddOrderItem(Guid productVariantId, decimal price, int quantity)
    {
        if (productVariantId == Guid.Empty)
            throw new ArgumentException("Product Variant ID cannot be empty.", nameof(productVariantId));

        if (price < 0)
            throw new ArgumentException("Price cannot be negative.", nameof(price));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        var orderItem = new OrderItem(this.Id, productVariantId, price, quantity);
        _orderItems.Add(orderItem);
        TotalAmount += (price * quantity);
    }
    public string? PaymentTransactionId { get; private set; }

    public void SetTransactionId(string transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            throw new ArgumentException("Transaction ID cannot be empty.");

        PaymentTransactionId = transactionId;
    }
    public void ChangeOrderStatus(OrderStatus newStatus)
    {
        if (Status == OrderStatus.Cancelled || Status == OrderStatus.Refunded)
        {
            throw new InvalidOperationException("Cannot change the status of a Cancelled or Refunded order!");
        }

        Status = newStatus;

        if (newStatus == OrderStatus.Refunded)
        {
            AddDomainEvent(new OrderRefundedEvent(Id, CustomerId, TotalAmount));
        }

        if (newStatus == OrderStatus.Cancelled)
        {
            AddDomainEvent(new OrderCancelledEvent(Id, CustomerId, TotalAmount));
        }

        if (newStatus == OrderStatus.Pending)
        {
            AddDomainEvent(new OrderPlacedEvent(Id, CustomerId, TotalAmount));
        }

        if (newStatus == OrderStatus.Delivered)
        {
            AddDomainEvent(new OrderDeliveredEvent(Id, CustomerId));
        }
    }
}
