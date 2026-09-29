using OmniMart.Domain.Common;
using OmniMart.Domain.Enums;
using OmniMart.Domain.Events;

namespace OmniMart.Domain.Entities;

public class Product:BaseEntity
{
    public Guid Id { get; private set; }
    public Guid VendorId { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public ProductStatus Status { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;
    private readonly List<ProductVariant> _variants = new();
    public virtual IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();
    private readonly List<ProductImage> _images = new();
    public virtual IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();
    public decimal BasePrice { get; private set; }
    public bool IsPublished { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    private readonly List<ProductReview> _reviews = new();
    public virtual IReadOnlyCollection<ProductReview> Reviews => _reviews.AsReadOnly();
    public virtual VendorProfile? VendorProfile { get; private set; }
    public virtual Category? Category { get; private set; }

    public Product(Guid vendorId, Guid categoryId, string name, string description, decimal basePrice, bool isPublished = false)
    {
        if (vendorId == Guid.Empty)
            throw new ArgumentException("VendorId cannot be empty.", nameof(vendorId));
        if (categoryId == Guid.Empty) throw new ArgumentException("Category ID cannot be empty.", nameof(categoryId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Product name is required.", nameof(name));
        if (basePrice < 0) throw new ArgumentException("Base price cannot be negative.", nameof(basePrice));
        if (isPublished && basePrice == 0) throw new ArgumentException("Published products must have a valid base price.", nameof(basePrice));
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
        VendorId = vendorId;
        CategoryId = categoryId;
        Name = name;
        Description = description;
        BasePrice = basePrice;
        IsPublished = isPublished;
        Status = ProductStatus.Draft;
        IsDeleted = false;
    }
#pragma warning disable CS8618
    protected Product() { }
#pragma warning restore CS8618

    public void UpdateDetails(string name, string description, decimal basePrice, Guid categoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Product description is required.", nameof(description));

        if (basePrice < 0)
            throw new ArgumentException("Base price cannot be negative.", nameof(basePrice));

        if (categoryId == Guid.Empty)
            throw new ArgumentException("Category ID cannot be empty.", nameof(categoryId));

        if (IsPublished && basePrice == 0)
            throw new ArgumentException("Published products must have a valid base price.", nameof(basePrice));

        Name = name;
        Description = description;
        BasePrice = basePrice;
        CategoryId = categoryId;

        if (Status == ProductStatus.Active || Status == ProductStatus.Rejected || Status == ProductStatus.Suspended)
        {
            Status = ProductStatus.PendingReview;
            IsPublished = false;
            PublishedAt = null;
            RejectionReason = null;
            SuspensionReason = null;
        }
    }
    public void SubmitForReview()
    {
        if (Status != ProductStatus.Draft)
            throw new InvalidOperationException("Only draft products can be submitted for review.");
        Status = ProductStatus.PendingReview;
    }
    public void ApproveProduct()
    {
        if (Status != ProductStatus.PendingReview)
            throw new InvalidOperationException("Only products pending review can be approved.");
        Status = ProductStatus.Active;
        AddDomainEvent(new ProductApprovedEvent(this.Id, this.VendorId));
    }
    public string? RejectionReason { get; private set; }
    public void RejectProduct(string reason)
    {
        if (Status != ProductStatus.PendingReview)
            throw new InvalidOperationException("Only products pending review can be rejected.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.", nameof(reason));

        Status = ProductStatus.Rejected;
        RejectionReason = reason;
        AddDomainEvent(new ProductRejectedEvent(this.Id, this.VendorId, reason));
    }
    public void SetPublishStatus(bool isPublished)
    {
        if (isPublished && Status != ProductStatus.Active)
            throw new InvalidOperationException("Only active products can be published.");
        if (isPublished && BasePrice == 0)
            throw new InvalidOperationException("Published products must have a valid base price.");
        IsPublished = isPublished;
        if (isPublished)
        {
            PublishedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            PublishedAt = null; 
        }
    }
    public void MarkAsDeleted()
    {
        IsDeleted = true;

        foreach (var variant in _variants)
        {
            variant.MarkAsDeleted();
        }
    }

    public void Restore()
    {
        IsDeleted = false;
        IsPublished = false;
        PublishedAt = null;
        Status = ProductStatus.Draft;

        foreach (var variant in _variants)
        {
            variant.Restore();
        }
    }

    public string? SuspensionReason { get; private set; }
    public void SuspendProduct(string reason)
    {
        if (Status != ProductStatus.Active)
            throw new InvalidOperationException("Only active products can be suspended.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Suspension reason is required.", nameof(reason));

        Status = ProductStatus.Suspended;
        SuspensionReason = reason;

        IsPublished = false;
        PublishedAt = null;
        AddDomainEvent(new ProductSuspendedEvent(this.Id, this.VendorId, reason));
    }

    public void ReactivateProduct()
    {
        if (Status != ProductStatus.Suspended)
            throw new InvalidOperationException("Only suspended products can be reactivated.");

        Status = ProductStatus.Active;
        SuspensionReason = null;
    }
    public void RemoveVariant(string sku)
    {
        var variant = _variants.FirstOrDefault(v => v.SKU.Equals(sku, StringComparison.OrdinalIgnoreCase));

        if (variant == null)
        {
            throw new InvalidOperationException("Variant not found.");
        }

        variant.MarkAsDeleted(); 
    }
    public void AddVariant(string sku, decimal price, int stockQuantity)
    {
        if (Status == ProductStatus.PendingReview || Status == ProductStatus.Suspended || Status == ProductStatus.Rejected)
        {
            throw new InvalidOperationException($"Cannot add variants to a product with status: {Status}");
        }

        var existingVariant = _variants.FirstOrDefault(s => s.SKU.Equals(sku, StringComparison.OrdinalIgnoreCase));

        if (existingVariant != null)
        {
            if (existingVariant.IsDeleted)
            {
                existingVariant.Restore(price, stockQuantity); 
                return;
            }

            throw new ArgumentException("A variant with the same SKU already exists and is active.", nameof(sku));
        }

        var newVariant = new ProductVariant(Id, sku, price, stockQuantity);
        _variants.Add(newVariant);
    }

    public void UpdateVariant(string sku, decimal price, int stockQuantity)
    {
        if (Status == ProductStatus.PendingReview || Status == ProductStatus.Suspended || Status == ProductStatus.Rejected)
        {
            throw new InvalidOperationException($"Cannot update variants for a product with status: {Status}");
        }

        var variant = _variants.FirstOrDefault(v => v.SKU.Equals(sku, StringComparison.OrdinalIgnoreCase));

        if (variant == null || variant.IsDeleted)
        {
            throw new InvalidOperationException("Active variant not found.");
        }

        variant.UpdateDetails(price, stockQuantity);
    }

    public Guid AddReview(Guid customerId, int rating, string? comment)
    {
        var newReview = new ProductReview(this.Id, customerId, rating, comment);
        _reviews.Add(newReview);
        AddDomainEvent(new ProductReviewedEvent(this.Id, newReview.Id));

        return newReview.Id; 
    }

    public void AddImage(string imageUrl, string publicId, bool isPrimary)
    {
        if (Status == ProductStatus.PendingReview || Status == ProductStatus.Suspended || Status == ProductStatus.Rejected)
        {
            throw new InvalidOperationException($"Cannot add images to a product with status: {Status}");
        }

        if (_images.Count >= 5)
            throw new InvalidOperationException("Cannot add more than 5 images to a product.");

        if (!_images.Any())
            isPrimary = true;
        else if (isPrimary)
        {
            foreach (var img in _images.Where(i => i.IsPrimary))
            {
                img.RemovePrimary();
            }
        }

        _images.Add(new ProductImage(this.Id, imageUrl, publicId, isPrimary));
    }

    public void RemoveImage(Guid imageId)
    {
        if (Status == ProductStatus.PendingReview || Status == ProductStatus.Suspended || Status == ProductStatus.Rejected)
        {
            throw new InvalidOperationException($"Cannot remove images from a product with status: {Status}");
        }

        var image = _images.FirstOrDefault(i => i.Id == imageId);
        if (image == null) throw new InvalidOperationException("Image not found.");

        _images.Remove(image);

        if (image.IsPrimary && _images.Any())
        {
            _images.First().SetAsPrimary();
        }
    }
    public void SetPrimaryImage(Guid imageId)
    {
        var imageExists = _images.Any(i => i.Id == imageId);
        if (!imageExists)
            throw new InvalidOperationException("The specified image does not exist in this product.");

        foreach (var img in _images)
        {
            img.SetPrimary(img.Id == imageId);
        }
    }
}
