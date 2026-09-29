using OmniMart.Domain.Common;
using OmniMart.Domain.Enums;
using OmniMart.Domain.Events;

namespace OmniMart.Domain.Entities;

public class ProductVariant:BaseEntity
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string SKU { get; private set; }
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public bool IsDeleted { get; private set; }
    private readonly List<VariantAttributeValue> _variantAttributeValues = new();
    public virtual IReadOnlyCollection<VariantAttributeValue> VariantAttributeValues => _variantAttributeValues.AsReadOnly();

    public virtual Product? Product { get; private set; }
    public virtual ICollection<OrderItem> OrderItems { get; private set; } = new List<OrderItem>();
    public virtual ICollection<CartItem> CartItems { get; private set; } = new List<CartItem>();

    public ProductVariant(Guid productId, string sku, decimal price, int stockQuantity)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty.", nameof(productId));
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU cannot be null or empty.", nameof(sku));
        if (price < 0)
            throw new ArgumentException("Price cannot be negative.", nameof(price));
        if (stockQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative.", nameof(stockQuantity));

        Id = Guid.NewGuid();
        ProductId = productId;
        SKU = sku;
        Price = price;
        StockQuantity = stockQuantity;
        IsDeleted = false;
    }

#pragma warning disable CS8618
    protected ProductVariant() { }
#pragma warning restore CS8618
    public void MarkAsDeleted()
    {
        IsDeleted = true;
    }

    public void Restore()
    {
        IsDeleted = false;
    }
    public void AddAttributeValue(Guid attributeId, string valueId)
    {
        if (_variantAttributeValues.Any(v => v.ProductAttributeId == attributeId))
        {
            throw new InvalidOperationException("This attribute has already been added to the variant.");
        }

        var newValue = new VariantAttributeValue(Id, attributeId, valueId);
        _variantAttributeValues.Add(newValue);
    }

    public void RemoveAttributeValue(Guid attributeId)
    {
        var attributeValue = _variantAttributeValues.FirstOrDefault(v => v.ProductAttributeId == attributeId);
        if (attributeValue != null)
        {
            _variantAttributeValues.Remove(attributeValue);
        }
    }

    public void Restore(decimal newPrice, int newStockQuantity)
    {
        if (newPrice < 0)
            throw new ArgumentException("Price cannot be negative.", nameof(newPrice));
        if (newStockQuantity < 0) 
            throw new ArgumentException("Stock quantity cannot be negative.", nameof(newStockQuantity));
        IsDeleted = false;
        Price = newPrice;
        StockQuantity = newStockQuantity;
    }

    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (StockQuantity < quantity)
            throw new InvalidOperationException("Insufficient stock to decrease.");
        StockQuantity -= quantity;
        if (StockQuantity == 0)
        {
            AddDomainEvent(new ProductOutOfStockEvent(this.Id, this.ProductId));
        }
    }
    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        StockQuantity += quantity;
    }
    public void UpdateDetails(decimal newPrice, int newStockQuantity)
    {
        if (newPrice < 0) throw new ArgumentException("Price cannot be negative.");
        if (newStockQuantity < 0) throw new ArgumentException("Stock cannot be negative.");

        Price = newPrice;
        StockQuantity = newStockQuantity;
    }

    public int ReservedStock { get; private set; }

    public int AvailableStock => StockQuantity - ReservedStock;

    public void ReserveStock(int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.");
        if (AvailableStock < quantity) throw new InvalidOperationException("Insufficient available stock to reserve.");

        ReservedStock += quantity;
    }

    public void CommitReservedStock(int quantity)
    {
        if (ReservedStock < quantity) throw new InvalidOperationException("Cannot commit more than reserved.");

        ReservedStock -= quantity;
        StockQuantity -= quantity;

        if (StockQuantity == 0)
        {
            AddDomainEvent(new Events.ProductOutOfStockEvent(this.Id, this.ProductId));
        }
    }

    public void ReleaseReservedStock(int quantity)
    {
        if (ReservedStock < quantity) throw new InvalidOperationException("Cannot release more than reserved.");

        ReservedStock -= quantity;
    }
}

