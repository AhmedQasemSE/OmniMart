namespace OmniMart.Domain.Entities;

public class CartItem
{
    public Guid Id { get; private set; }
    public Guid CartId { get; private set; }
    public Guid ProductVariantId { get; private set; }
    public int Quantity { get; private set; }
     
    public virtual ProductVariant? ProductVariant { get; private set; }
    public virtual Cart? Cart { get; private set; }
    public CartItem( Guid cartId, Guid productVariantId, int quantity )
    {
        if (cartId == Guid.Empty)
            throw new ArgumentException("Cart ID cannot be empty.", nameof(cartId));
        if (productVariantId == Guid.Empty)
            throw new ArgumentException("ProductVariant ID cannot be empty.", nameof(productVariantId));
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        Id = Guid.NewGuid();
        CartId = cartId;
        ProductVariantId = productVariantId;
        Quantity = quantity;
    }
#pragma warning disable CS8618
    protected CartItem() { }
#pragma warning restore CS8618
    public void AddQuantity(int additionalQuantity) 
    {
        if (additionalQuantity <= 0)
            throw new ArgumentException("Quantity to add must be greater than zero.", nameof(additionalQuantity));

        Quantity += additionalQuantity;
    }
    public void DecreaseQuantity(int amountToRemove)
    {
        if (amountToRemove <= 0)
            throw new ArgumentException("Amount to remove must be positive.");

        if (Quantity - amountToRemove < 0)
            throw new InvalidOperationException("Cannot decrease quantity below zero.");

        Quantity -= amountToRemove;
    }

    public void SetQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        Quantity = newQuantity;
    }
}
