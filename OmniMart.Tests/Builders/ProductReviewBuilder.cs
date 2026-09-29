using OmniMart.Domain.Entities;
using System;
using System.Reflection;

namespace OmniMart.Tests.Builders;

public class ProductReviewBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _productId = Guid.NewGuid();
    private Guid _customerId = Guid.NewGuid();
    private int _rating = 5;
    private string? _comment = "Excellent product!";
    private DateTime _createdAt = DateTime.UtcNow;

    private Product? _product = null;
    private CustomerProfile? _customer = null;

    public ProductReviewBuilder WithId(Guid id) { _id = id; return this; }
    public ProductReviewBuilder WithRating(int rating) { _rating = rating; return this; }
    public ProductReviewBuilder WithComment(string? comment) { _comment = comment; return this; }
    public ProductReviewBuilder WithCreatedAt(DateTime createdAt) { _createdAt = createdAt; return this; }

    public ProductReviewBuilder WithProduct(Product product)
    {
        _product = product;
        _productId = product.Id;
        return this;
    }

    public ProductReviewBuilder WithCustomer(CustomerProfile customer)
    {
        _customer = customer;
        _customerId = customer.Id;
        return this;
    }

    public ProductReview Build()
    {
        var review = new ProductReview(_productId, _customerId, _rating, _comment);

        SetPrivateProperty(review, "Id", _id);
        SetPrivateProperty(review, "CreatedAt", _createdAt);

        if (_product != null) SetPrivateProperty(review, "Product", _product);
        if (_customer != null) SetPrivateProperty(review, "Customer", _customer);

        return review;
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