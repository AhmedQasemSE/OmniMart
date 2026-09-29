using OmniMart.Domain.Common;
using OmniMart.Domain.Enums;

namespace OmniMart.Domain.Entities;

public class PaymentGroup : BaseEntity
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }

    public decimal TotalAmount { get; private set; }
    public string? StripeSessionId { get; private set; }
    public bool IsPaid { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public virtual Order? Order { get; private set; }
    private readonly List<Order> _orders = new();
    public virtual IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();

    public PaymentGroup(Guid customerId)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID cannot be empty.");

        Id = Guid.NewGuid();
        CustomerId = customerId;
        TotalAmount = 0;
        IsPaid = false;
        CreatedAt = DateTimeOffset.UtcNow;
    }

#pragma warning disable CS8618
    protected PaymentGroup() { }
#pragma warning restore CS8618

    public void AddOrder(Order order)
    {
        _orders.Add(order);
        TotalAmount += order.TotalAmount;
    }

    public void MarkAsPaid(string stripeSessionId)
    {
        IsPaid = true;
        StripeSessionId = stripeSessionId;

        foreach (var order in _orders)
        {
            order.ChangeOrderStatus(OrderStatus.Processing);
            order.SetTransactionId(stripeSessionId);
        }
    }
    public void MarkAsFailed()
    {
        if (IsPaid) return; 

        foreach (var order in _orders)
        {
            order.ChangeOrderStatus(OrderStatus.Cancelled);
        }
    }
}