using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OmniMart.Tests.Builders;

public class ProductBuilder
{
    private Guid _vendorId = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();
    private Guid _categoryId = Guid.NewGuid();
    private Guid? _productId = null;
    private string _name = "Test Product";
    private bool _isPublished = true;
    private ProductStatus _status = ProductStatus.Active;
    private bool _isVendorApproved = true;
    private bool _isDeleted = false;

    private readonly List<(string SKU, decimal Price, int StockQuantity)> _variants = new();
    private readonly List<(Guid Id, string Url, string PublicId, bool IsPrimary)> _images = new();
    private VendorProfile? _customVendorProfile = null;
    private decimal _basePrice = 100m;

    public ProductBuilder WithName(string name) { _name = name; return this; }
    public ProductBuilder WithCategory(Guid categoryId) { _categoryId = categoryId; return this; }
    public ProductBuilder WithId(Guid productId) { _productId = productId; return this; }
    public ProductBuilder AsDeleted() { _isDeleted = true; return this; }
    public ProductBuilder WithStatus(ProductStatus status) { _status = status; return this; }
    public ProductBuilder IsPublished(bool isPublished) { _isPublished = isPublished; return this; }
    public ProductBuilder IsVendorApproved(bool approved) { _isVendorApproved = approved; return this; }
    public ProductBuilder WithBasePrice(decimal basePrice) { _basePrice = basePrice; return this; }

    public ProductBuilder WithVendorProfile(VendorProfile vendor)
    {
        _customVendorProfile = vendor;
        _vendorId = vendor.Id;
        _userId = vendor.UserId;
        return this;
    }

    public ProductBuilder WithVendorIds(Guid vendorId, Guid userId)
    {
        _vendorId = vendorId;
        _userId = userId;
        return this;
    }
    public ProductBuilder WithCategoryId(Guid categoryId)
    {
        _categoryId = categoryId;
        return this;
    }
    public ProductBuilder WithVariant(string sku, decimal price, int stockQuantity)
    {
        _variants.Add((sku, price, stockQuantity));
        return this;
    }
    public ProductBuilder WithImage(Guid imageId, string url = "http://dummy.com/img.png", string publicId = "pub_123", bool isPrimary = false)
    {
        _images.Add((imageId, url, publicId, isPrimary));
        return this;
    }
    public ProductBuilder WithPrimaryImage(Guid imageId, string url = "http://dummy.com/img.png", string publicId = "pub_123")
    {
        _images.Add((imageId, url, publicId, true));
        return this;
    }
    public Product Build()
    {
        var vendor = _customVendorProfile;
        if (vendor == null)
        {
            vendor = new VendorProfile(_userId, "Test Store", "CR-123456", 10m, "V-1234");
            SetPrivateProperty(vendor, "Id", _vendorId);

            if (_isVendorApproved && !vendor.IsApproved)
            {
                vendor.ApproveAccount();
            }
        }

        var product = new Product(vendor.Id, _categoryId, _name, "Default Description", _basePrice, false);

        if (_productId.HasValue)
        {
            SetPrivateProperty(product, "Id", _productId.Value);
        }

        if (_isDeleted)
        {
            SetPrivateProperty(product, "IsDeleted", true);
        }

        SetPrivateProperty(product, "VendorProfile", vendor);

        if (_status == ProductStatus.PendingReview)
            product.SubmitForReview();
        else if (_status == ProductStatus.Active)
        { product.SubmitForReview(); product.ApproveProduct(); }
        else if (_status == ProductStatus.Rejected)
        { product.SubmitForReview(); product.RejectProduct("Rejected"); }
        else if (_status == ProductStatus.Suspended)
        { product.SubmitForReview(); product.ApproveProduct(); product.SuspendProduct("Suspended"); }

        if (_isPublished && _status == ProductStatus.Active)
        {
            product.SetPublishStatus(true);
        }

        foreach (var v in _variants)
        {
            product.AddVariant(v.SKU, v.Price, v.StockQuantity);
        }

        foreach (var variant in product.Variants)
        {
            SetPrivateProperty(variant, "Product", product);
        }

        foreach (var imgData in _images)
        {
            product.AddImage(imgData.Url, imgData.PublicId, imgData.IsPrimary);

            var addedImage = product.Images.LastOrDefault();
            if (addedImage != null)
            {
                SetPrivateProperty(addedImage, "Id", imgData.Id);
            }
        }

        return product;
    }

    private void SetPrivateProperty(object instance, string propertyName, object value)
    {
        var type = instance.GetType();
        PropertyInfo? property = null;

        while (type != null && property == null)
        {
            property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            type = type.BaseType;
        }

        property?.SetValue(instance, value);
    }
}