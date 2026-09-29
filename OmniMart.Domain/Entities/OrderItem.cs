namespace OmniMart.Domain.Entities
{
    public class OrderItem
    {
        public Guid Id { get; private set; }
        public Guid OrderId { get; private set; }
        public Guid ProductVariantId { get; private set; }
        public int Quantity { get; private set; }
        public decimal Price { get; private set; }
         
        public virtual ProductVariant? ProductVariant { get; private set; }
        public virtual Order? Order { get; private set; }
        public OrderItem(Guid orderId, Guid productVariantId, decimal price, int quantity )
        {
            if (orderId == Guid.Empty) throw new ArgumentException("Order  ID cannot be empty. ",nameof(orderId));
            if (productVariantId == Guid.Empty) throw new ArgumentException("ProductVariantId  ID cannot be empty. ", nameof(productVariantId));
            if (price <= 0) throw new ArgumentException("Price cannot be negative.", nameof(price));
            if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
            Id = Guid.NewGuid();
            OrderId = orderId;
            ProductVariantId = productVariantId;
            Quantity = quantity;
            Price = price;
        }

#pragma warning disable CS8618
        protected OrderItem() { }
#pragma warning restore CS8618

    }
}
