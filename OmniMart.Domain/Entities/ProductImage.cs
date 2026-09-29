using OmniMart.Domain.Common;

namespace OmniMart.Domain.Entities;

public class ProductImage : BaseEntity
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }

    public string ImageUrl { get; private set; }

    public string PublicId { get; private set; }

    public bool IsPrimary { get; private set; }

    public virtual Product? Product { get; private set; }

    public ProductImage(Guid productId, string imageUrl, string publicId, bool isPrimary = false)
    {
        if (productId == Guid.Empty) throw new ArgumentException("Product ID is required.");
        if (string.IsNullOrWhiteSpace(imageUrl)) throw new ArgumentException("Image URL is required.");
        if (string.IsNullOrWhiteSpace(publicId)) throw new ArgumentException("Public ID is required.");

        Id = Guid.NewGuid();
        ProductId = productId;
        ImageUrl = imageUrl;
        PublicId = publicId;
        IsPrimary = isPrimary;
    }

#pragma warning disable CS8618
    protected ProductImage() { }
#pragma warning restore CS8618

    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    public void RemovePrimary()
    {
        IsPrimary = false;
    }
    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
    }
}