namespace OmniMart.Domain.Entities;

public class Cart
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }

    private readonly List<CartItem> _cartItems = new();
    public virtual IReadOnlyCollection<CartItem> CartItems => _cartItems.AsReadOnly();

    public virtual CustomerProfile? Customer { get; private set; }

    public Cart(Guid customerId)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID cannot be empty.", nameof(customerId));

        Id = Guid.NewGuid();
        CustomerId = customerId;
    }

#pragma warning disable CS8618
    protected Cart() { }
#pragma warning restore CS8618

    public void AddItem(Guid productVariantId, int quantity)
    {
        if (productVariantId == Guid.Empty)
            throw new ArgumentException("ProductVariant ID cannot be empty.", nameof(productVariantId));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        var existingItem = _cartItems.FirstOrDefault(i => i.ProductVariantId == productVariantId);

        if (existingItem != null)
        {
            existingItem.AddQuantity(quantity);
        }
        else
        {
            _cartItems.Add(new CartItem(Id, productVariantId, quantity));
        }
    }

    public void DecreaseItemQuantity(Guid productVariantId, int amountToRemove)
    {
        var itemToRemove = _cartItems.FirstOrDefault(i => i.ProductVariantId == productVariantId);

        if (itemToRemove == null)
            throw new InvalidOperationException("Item not found in the cart.");

        if (itemToRemove.Quantity <= amountToRemove)
        {
            _cartItems.Remove(itemToRemove);
        }
        else
        {
            itemToRemove.DecreaseQuantity(amountToRemove);
        }
    }
    public void UpdateItemQuantity(Guid productVariantId, int newQuantity)
    {
        var item = _cartItems.FirstOrDefault(i => i.ProductVariantId == productVariantId);

        if (item == null)
            throw new InvalidOperationException("Item not found in the cart.");

        item.SetQuantity(newQuantity);
    }
    public void RemoveItem(Guid productVariantId)
    {
        var item = _cartItems.FirstOrDefault(i => i.ProductVariantId == productVariantId);
        if (item != null)
        {
            _cartItems.Remove(item);
        }
    }

    public void ClearCart()
    {
        _cartItems.Clear();
    }
}