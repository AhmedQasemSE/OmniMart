using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using System;
using System.Reflection;

namespace OmniMart.Tests.Builders;

public class ProductVariantBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _productId = Guid.NewGuid();
    private string _sku = "SKU-12345";
    private decimal _price = 100m;
    private int _stockQuantity = 10;
    private int _reservedStock = 0;
    private bool _isDeleted = false;
    private Product? _product = null;

    public ProductVariantBuilder WithId(Guid id) { _id = id; return this; }
    public ProductVariantBuilder WithProductId(Guid productId) { _productId = productId; return this; }
    public ProductVariantBuilder WithSKU(string sku) { _sku = sku; return this; }
    public ProductVariantBuilder WithPrice(decimal price) { _price = price; return this; }
    public ProductVariantBuilder WithStockQuantity(int quantity) { _stockQuantity = quantity; return this; }
    public ProductVariantBuilder WithReservedStock(int quantity) { _reservedStock = quantity; return this; }
    public ProductVariantBuilder AsDeleted() { _isDeleted = true; return this; }

    public ProductVariantBuilder WithProductEntity(Product product)
    {
        _product = product;
        return this;
    }

    public ProductVariantBuilder WithCustomProductStatus(bool isPublished, ProductStatus status, bool isVendorApproved)
    {
        _product = new ProductBuilder()
            .WithId(_productId)
            .IsPublished(isPublished)
            .WithStatus(status)
            .IsVendorApproved(isVendorApproved)
            .Build();
        return this;
    }

    public ProductVariantBuilder WithValidActiveProductAndVendor()
    {
        return WithCustomProductStatus(true, ProductStatus.Active, true);
    }

    public ProductVariant Build()
    {
        var variant = new ProductVariant(_productId, _sku, _price, _stockQuantity);

        typeof(ProductVariant).GetProperty("Id", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(variant, _id);

        if (_isDeleted)
        {
            typeof(ProductVariant).GetProperty("IsDeleted", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(variant, true);
        }

        if (_reservedStock > 0)
        {
            typeof(ProductVariant).GetProperty("ReservedStock", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(variant, _reservedStock);
        }

        if (_product != null)
        {
            typeof(ProductVariant).GetProperty("Product", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(variant, _product);
        }

        return variant;
    }
}