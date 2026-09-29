using FluentAssertions;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using OmniMart.Domain.Events;
using System;
using System.Linq;
using Xunit;

namespace OmniMart.Tests.Domain;

public class ProductTests
{
    private readonly Guid _validVendorId = Guid.NewGuid();
    private readonly Guid _validCategoryId = Guid.NewGuid();
    private readonly Product _draftProduct;

    public ProductTests()
    {
        _draftProduct = GetDraft();
    }

    #region Helper Methods

    private Product GetDraft() => new Product(_validVendorId, _validCategoryId, "Laptop", "Gaming Laptop", 1500m);

    private Product GetPending() { var p = GetDraft(); p.SubmitForReview(); return p; }

    private Product GetActive() { var p = GetPending(); p.ApproveProduct(); return p; }

    private Product GetRejected() { var p = GetPending(); p.RejectProduct("Admin Rejected"); return p; }

    private Product GetSuspended() { var p = GetActive(); p.SuspendProduct("Policy Violation"); return p; }


    private Product GetProductInStatus(ProductStatus status) => status switch
    {
        ProductStatus.Draft => GetDraft(),
        ProductStatus.PendingReview => GetPending(),
        ProductStatus.Active => GetActive(),
        ProductStatus.Rejected => GetRejected(),
        ProductStatus.Suspended => GetSuspended(),
        _ => throw new NotImplementedException()
    };

    #endregion

    #region 1. Constructor Tests 

    [Fact]
    public void Constructor_WhenDataIsValid_ShouldCreateProductInDraftStatus()
    {
        var product = new Product(_validVendorId, _validCategoryId, "Laptop", "Gaming Laptop", 1500m);

        product.Id.Should().NotBeEmpty();
        product.VendorId.Should().Be(_validVendorId);
        product.CategoryId.Should().Be(_validCategoryId);
        product.Name.Should().Be("Laptop");
        product.BasePrice.Should().Be(1500m);

        product.Status.Should().Be(ProductStatus.Draft);
        product.IsPublished.Should().BeFalse();
        product.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WhenVendorIdIsEmpty_ShouldThrowArgumentException()
    {
        Action act = () => new Product(Guid.Empty, _validCategoryId, "Name", "Desc", 100m);
        act.Should().Throw<ArgumentException>().WithMessage("*VendorId cannot be empty*");
    }

    [Fact]
    public void Constructor_WhenCategoryIdIsEmpty_ShouldThrowArgumentException()
    {
        Action act = () => new Product(_validVendorId, Guid.Empty, "Name", "Desc", 100m);
        act.Should().Throw<ArgumentException>().WithMessage("*Category ID cannot be empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WhenNameIsInvalid_ShouldThrowArgumentException(string? invalidName)
    {
        Action act = () => new Product(_validVendorId, _validCategoryId, invalidName!, "Desc", 100m);
        act.Should().Throw<ArgumentException>().WithMessage("*Product name is required*");
    }

    [Fact]
    public void Constructor_WhenBasePriceIsNegative_ShouldThrowArgumentException()
    {
        Action act = () => new Product(_validVendorId, _validCategoryId, "Name", "Desc", -50m);
        act.Should().Throw<ArgumentException>().WithMessage("*Base price cannot be negative*");
    }

    [Fact]
    public void Constructor_WhenIsPublishedTrueButPriceIsZero_ShouldThrowArgumentException()
    {
        Action act = () => new Product(_validVendorId, _validCategoryId, "Name", "Desc", 0m, isPublished: true);
        act.Should().Throw<ArgumentException>().WithMessage("*Published products must have a valid base price*");
    }

    #endregion

    #region 2. UpdateDetails Tests

    [Fact]
    public void UpdateDetails_WhenDataIsValid_ShouldUpdateProperties()
    {
        var newCategoryId = Guid.NewGuid();

        _draftProduct.UpdateDetails("New Name", "New Desc", 200m, newCategoryId);

        _draftProduct.Name.Should().Be("New Name");
        _draftProduct.Description.Should().Be("New Desc");
        _draftProduct.BasePrice.Should().Be(200m);
        _draftProduct.CategoryId.Should().Be(newCategoryId);
    }
    [Theory]
    [InlineData(ProductStatus.Active)]
    [InlineData(ProductStatus.Rejected)]
    [InlineData(ProductStatus.Suspended)]
    public void UpdateDetails_WhenProductIsNotDraft_ShouldResetStatusToPendingReviewAndUnpublish(ProductStatus initialStatus)
    {
        var product = GetProductInStatus(initialStatus);

        product.UpdateDetails("Updated Name", "Updated Desc", 150m, _validCategoryId);

        product.Status.Should().Be(ProductStatus.PendingReview);
        product.IsPublished.Should().BeFalse();
        product.PublishedAt.Should().BeNull();
        product.RejectionReason.Should().BeNull();
        product.SuspensionReason.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdateDetails_WhenNameIsInvalid_ShouldThrowArgumentException(string? invalidName)
    {
        Action act = () => _draftProduct.UpdateDetails(invalidName!, "New Desc", 200m, _validCategoryId);
        act.Should().Throw<ArgumentException>().WithMessage("*Product name is required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdateDetails_WhenDescriptionIsInvalid_ShouldThrowArgumentException(string? invalidDesc)
    {
        Action act = () => _draftProduct.UpdateDetails("New Name", invalidDesc!, 200m, _validCategoryId);
        act.Should().Throw<ArgumentException>().WithMessage("*Product description is required*");
    }

    [Fact]
    public void UpdateDetails_WhenBasePriceIsNegative_ShouldThrowArgumentException()
    {
        Action act = () => _draftProduct.UpdateDetails("New Name", "New Desc", -5m, _validCategoryId);
        act.Should().Throw<ArgumentException>().WithMessage("*Base price cannot be negative*");
    }

    [Fact]
    public void UpdateDetails_WhenCategoryIdIsEmpty_ShouldThrowArgumentException()
    {
        Action act = () => _draftProduct.UpdateDetails("New Name", "New Desc", 200m, Guid.Empty);
        act.Should().Throw<ArgumentException>().WithMessage("*Category ID cannot be empty*");
    }

    [Fact]
    public void UpdateDetails_WhenIsPublishedTrueButPriceIsZero_ShouldThrowArgumentException()
    {
        var product = GetActive();
        product.SetPublishStatus(true); 

        Action act = () => product.UpdateDetails("New Name", "New Desc", 0m, _validCategoryId);
        act.Should().Throw<ArgumentException>().WithMessage("*Published products must have a valid base price*");
    }

    #endregion
    #region 3. State Machine Tests 


    [Fact]
    public void SubmitForReview_WhenStatusIsDraft_ShouldChangeToPendingReview()
    {
        _draftProduct.SubmitForReview();
        _draftProduct.Status.Should().Be(ProductStatus.PendingReview);
    }

    [Fact]
    public void SubmitForReview_WhenStatusIsNotDraft_ShouldThrowInvalidOperationException()
    {
        var product = GetActive();

        Action act = () => product.SubmitForReview();

        act.Should().Throw<InvalidOperationException>().WithMessage("*Only draft products can be submitted for review.*");
    }


    [Fact]
    public void ApproveProduct_WhenStatusIsPendingReview_ShouldChangeToActive_AndRaiseEvent()
    {
        var product = GetPending();

        product.ApproveProduct();

        product.Status.Should().Be(ProductStatus.Active);
        product.DomainEvents.Should().ContainSingle(e => e is ProductApprovedEvent);
    }

    [Fact]
    public void ApproveProduct_WhenStatusIsNotPendingReview_ShouldThrowInvalidOperationException()
    {

        Action act = () => _draftProduct.ApproveProduct();

        act.Should().Throw<InvalidOperationException>().WithMessage("*Only products pending review can be approved.*");
    }


    [Fact]
    public void RejectProduct_WhenValid_ShouldChangeToRejected_AndRaiseEvent()
    {
        var product = GetPending();

        product.RejectProduct("Image quality is too low");

        product.Status.Should().Be(ProductStatus.Rejected);
        product.RejectionReason.Should().Be("Image quality is too low");
        product.DomainEvents.Should().ContainSingle(e => e is ProductRejectedEvent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void RejectProduct_WhenReasonIsInvalid_ShouldThrowArgumentException(string? invalidReason)
    {
        var product = GetPending();

        Action act = () => product.RejectProduct(invalidReason!);

        act.Should().Throw<ArgumentException>().WithMessage("*Rejection reason is required.*");
    }

    [Theory]
    [InlineData(ProductStatus.Draft)]
    [InlineData(ProductStatus.Active)]
    [InlineData(ProductStatus.Rejected)]
    [InlineData(ProductStatus.Suspended)]
    public void RejectProduct_WhenStatusIsNotPendingReview_ShouldThrowInvalidOperationException(ProductStatus invalidStatus)
    {
        var product = GetProductInStatus(invalidStatus);

        Action act = () => product.RejectProduct("Violates standards.");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Only products pending review can be rejected.*");
    }

    [Theory]
    [InlineData(ProductStatus.Draft)]
    [InlineData(ProductStatus.PendingReview)]
    [InlineData(ProductStatus.Rejected)]
    [InlineData(ProductStatus.Suspended)]
    public void SetPublishStatus_WhenStatusIsNotActive_ShouldThrowInvalidOperationException(ProductStatus invalidStatus)
    {
        var product = GetProductInStatus(invalidStatus);

        Action act = () => product.SetPublishStatus(true);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Only active products can be published.*");
    }

    [Fact]
    public void SetPublishStatus_WhenValidAndTrue_ShouldPublish_AndSetPublishedAtToCurrentTime()
    {
        var product = GetActive();

        product.SetPublishStatus(true);

        product.IsPublished.Should().BeTrue();
        product.PublishedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SetPublishStatus_WhenValidAndFalse_ShouldUnpublish_AndClearPublishedAt()
    {
        var product = GetActive();
        product.SetPublishStatus(true);

        product.SetPublishStatus(false);

        product.IsPublished.Should().BeFalse();
        product.PublishedAt.Should().BeNull();
    }

    [Fact]
    public void SetPublishStatus_WhenPriceIsZero_ShouldThrowInvalidOperationException()
    {
        var product = GetDraft();

        product.UpdateDetails("Name", "Desc", 0m, _validCategoryId);

        product.SubmitForReview();
        product.ApproveProduct();

        Action act = () => product.SetPublishStatus(true);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Published products must have a valid base price.*");
    }


    [Fact]
    public void SuspendProduct_WhenValid_ShouldChangeToSuspended_Unpublish_AndRaiseEvent()
    {
        var product = GetActive();
        product.SetPublishStatus(true); 

        product.SuspendProduct("Violation of terms");

        product.Status.Should().Be(ProductStatus.Suspended);
        product.SuspensionReason.Should().Be("Violation of terms");
        product.IsPublished.Should().BeFalse();
        product.PublishedAt.Should().BeNull();
        product.DomainEvents.Should().Contain(e => e is ProductSuspendedEvent);
    }

    [Theory]
    [InlineData(ProductStatus.Draft)]
    [InlineData(ProductStatus.PendingReview)]
    [InlineData(ProductStatus.Rejected)]
    [InlineData(ProductStatus.Suspended)]
    public void SuspendProduct_WhenStatusIsNotActive_ShouldThrowInvalidOperationException(ProductStatus invalidStatus)
    {
        var product = GetProductInStatus(invalidStatus);

        Action act = () => product.SuspendProduct("Violation of terms");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Only active products can be suspended.*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SuspendProduct_WhenReasonIsInvalid_ShouldThrowArgumentException(string? invalidReason)
    {
        var product = GetActive();

        Action act = () => product.SuspendProduct(invalidReason!);

        act.Should().Throw<ArgumentException>().WithMessage("*Suspension reason is required.*");
    }


    [Fact]
    public void ReactivateProduct_WhenSuspended_ShouldChangeToActive_AndClearReason()
    {
        var product = GetSuspended();

        product.ReactivateProduct();

        product.Status.Should().Be(ProductStatus.Active);
        product.SuspensionReason.Should().BeNull();
    }

    [Theory]
    [InlineData(ProductStatus.Draft)]
    [InlineData(ProductStatus.PendingReview)]
    [InlineData(ProductStatus.Active)]
    [InlineData(ProductStatus.Rejected)]
    public void ReactivateProduct_WhenStatusIsNotSuspended_ShouldThrowInvalidOperationException(ProductStatus invalidStatus)
    {
        var product = GetProductInStatus(invalidStatus);

        Action act = () => product.ReactivateProduct();

        act.Should().Throw<InvalidOperationException>().WithMessage("*Only suspended products can be reactivated.*");
    }

    #endregion

    #region 4. Managing Product Variants Tests 


    [Fact]
    public void AddVariant_WhenValid_ShouldAddVariantToCollection()
    {
        var product = GetActive(); 

        product.AddVariant("SKU-100", 150m, 50);

        product.Variants.Should().ContainSingle();
        var variant = product.Variants.First();
        variant.SKU.Should().Be("SKU-100");
    }

    [Theory]
    [InlineData(ProductStatus.PendingReview)]
    [InlineData(ProductStatus.Suspended)]
    [InlineData(ProductStatus.Rejected)]
    public void AddVariant_WhenStatusIsInvalid_ShouldThrowInvalidOperationException(ProductStatus invalidStatus)
    {
        var product = GetProductInStatus(invalidStatus);

        Action act = () => product.AddVariant("SKU-100", 150m, 50);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage($"Cannot add variants to a product with status: {invalidStatus}");
    }

    [Fact]
    public void AddVariant_WhenSkuAlreadyExistsAndIsActive_ShouldThrowArgumentException()
    {
        var product = GetActive();
        product.AddVariant("SKU-100", 150m, 50);

        Action act = () => product.AddVariant("SKU-100", 200m, 10);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*A variant with the same SKU already exists and is active.*");
    }

    [Fact]
    public void AddVariant_WhenSkuExistsButIsDeleted_ShouldRestoreVariant()
    {
        var product = GetActive();
        product.AddVariant("SKU-100", 150m, 50);
        product.RemoveVariant("SKU-100");

        product.AddVariant("SKU-100", 200m, 30);

        var variant = product.Variants.Single(v => v.SKU.Equals("SKU-100", StringComparison.OrdinalIgnoreCase));
        variant.IsDeleted.Should().BeFalse();
    }


    [Fact]
    public void UpdateVariant_WhenValid_ShouldCompleteSuccessfully()
    {
        var product = GetActive();
        product.AddVariant("SKU-100", 150m, 50);

        Action act = () => product.UpdateVariant("SKU-100", 200m, 70);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(ProductStatus.PendingReview)]
    [InlineData(ProductStatus.Suspended)]
    [InlineData(ProductStatus.Rejected)]
    public void UpdateVariant_WhenStatusIsInvalid_ShouldThrowInvalidOperationException(ProductStatus invalidStatus)
    {
        var product = GetProductInStatus(invalidStatus);

        Action act = () => product.UpdateVariant("SKU-100", 150m, 50);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage($"Cannot update variants for a product with status: {invalidStatus}");
    }

    [Fact]
    public void UpdateVariant_WhenVariantNotFound_ShouldThrowInvalidOperationException()
    {
        var product = GetActive();

        Action act = () => product.UpdateVariant("SKU-NOT-FOUND", 150m, 50);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Active variant not found.");
    }

    [Fact]
    public void UpdateVariant_WhenVariantIsDeleted_ShouldThrowInvalidOperationException()
    {
        var product = GetActive();
        product.AddVariant("SKU-100", 150m, 50);
        product.RemoveVariant("SKU-100");

        Action act = () => product.UpdateVariant("SKU-100", 150m, 50);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Active variant not found.");
    }


    [Fact]
    public void RemoveVariant_WhenValid_ShouldMarkVariantAsDeleted()
    {
        var product = GetActive();
        product.AddVariant("SKU-100", 150m, 50);

        product.RemoveVariant("SKU-100");

        var variant = product.Variants.First();
        variant.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void RemoveVariant_WhenVariantNotFound_ShouldThrowInvalidOperationException()
    {
        var product = GetActive();

        Action act = () => product.RemoveVariant("SKU-NOT-FOUND");

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Variant not found.");
    }

    #endregion

    #region 5. Reviews Tests 

    [Fact]
    public void AddReview_WhenValid_ShouldAddReviewAndRaiseEvent()
    {
        var product = GetActive();
        var customerId = Guid.NewGuid();

        var reviewId = product.AddReview(customerId, 5, "Great product!");

        product.Reviews.Should().ContainSingle();
        reviewId.Should().NotBeEmpty();
        product.DomainEvents.Should().Contain(e => e is ProductReviewedEvent);
    }

    #endregion
    #region 6. Images Management Tests


    [Fact]
    public void AddImage_WhenAddingFirstImage_ShouldForceItToBePrimary()
    {
        var product = GetActive(); 

        product.AddImage("url1", "id1", isPrimary: false);

        product.Images.Should().ContainSingle();
        product.Images.First().IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void AddImage_WhenAddingNewPrimaryImage_ShouldRemovePrimaryFromOldImages()
    {
        var product = GetActive();
        product.AddImage("url1", "id1", isPrimary: true);

        product.AddImage("url2", "id2", isPrimary: true);

        var oldImage = product.Images.First(i => i.PublicId == "id1");
        var newImage = product.Images.First(i => i.PublicId == "id2");

        oldImage.IsPrimary.Should().BeFalse();
        newImage.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void AddImage_WhenLimitExceeded_ShouldThrowInvalidOperationException()
    {
        var product = GetActive();

        product.AddImage("url1", "id1", isPrimary: true);
        product.AddImage("url2", "id2", isPrimary: false);
        product.AddImage("url3", "id3", isPrimary: false);
        product.AddImage("url4", "id4", isPrimary: false);
        product.AddImage("url5", "id5", isPrimary: false);

        Action act = () => product.AddImage("url6", "id6", isPrimary: false);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Cannot add more than 5 images to a product.");
    }

    [Theory]
    [InlineData(ProductStatus.PendingReview)]
    [InlineData(ProductStatus.Suspended)]
    [InlineData(ProductStatus.Rejected)]
    public void AddImage_WhenStatusIsInvalid_ShouldThrowInvalidOperationException(ProductStatus invalidStatus)
    {
        var product = GetProductInStatus(invalidStatus);

        Action act = () => product.AddImage("url", "id", isPrimary: true);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage($"Cannot add images to a product with status: {invalidStatus}");
    }


    [Fact]
    public void RemoveImage_WhenRemovingPrimaryImage_ShouldMakeNextImagePrimary()
    {
        var product = GetActive();
        product.AddImage("url1", "id1", isPrimary: true);
        product.AddImage("url2", "id2", isPrimary: false);
        var primaryImageId = product.Images.First(i => i.PublicId == "id1").Id;

        product.RemoveImage(primaryImageId);

        product.Images.Should().ContainSingle();
        product.Images.First().IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void RemoveImage_WhenRemovingNonPrimaryImage_ShouldNotAffectPrimaryImage()
    {
        var product = GetActive();
        product.AddImage("url1", "id1", isPrimary: true);
        product.AddImage("url2", "id2", isPrimary: false);
        var nonPrimaryImageId = product.Images.First(i => i.PublicId == "id2").Id;

        product.RemoveImage(nonPrimaryImageId);

        product.Images.Should().ContainSingle();
        product.Images.First().IsPrimary.Should().BeTrue();
    }
   
    [Theory]
    [InlineData(ProductStatus.PendingReview)]
    [InlineData(ProductStatus.Suspended)]
    [InlineData(ProductStatus.Rejected)]
    public void RemoveImage_WhenStatusIsInvalid_ShouldThrowInvalidOperationException(ProductStatus invalidStatus)
    {
        var product = GetProductInStatus(invalidStatus);

        Action act = () => product.RemoveImage(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
           .WithMessage($"Cannot remove images from a product with status: {invalidStatus}");
    }

    [Fact]
    public void RemoveImage_WhenImageDoesNotExist_ShouldThrowInvalidOperationException()
    {
        var product = GetActive();

        Action act = () => product.RemoveImage(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Image not found.");
    }


    [Fact]
    public void SetPrimaryImage_WhenValid_ShouldChangePrimaryImage()
    {
        var product = GetActive();
        product.AddImage("url1", "id1", isPrimary: true);
        product.AddImage("url2", "id2", isPrimary: false);
        var secondImageId = product.Images.First(i => i.PublicId == "id2").Id;

        product.SetPrimaryImage(secondImageId);

        product.Images.First(i => i.Id == secondImageId).IsPrimary.Should().BeTrue();
        product.Images.First(i => i.PublicId == "id1").IsPrimary.Should().BeFalse();
    }

    [Fact]
    public void SetPrimaryImage_WhenImageDoesNotExist_ShouldThrowInvalidOperationException()
    {
        var product = GetActive();

        Action act = () => product.SetPrimaryImage(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("The specified image does not exist in this product.");
    }

    #endregion

    #region 7. MarkAsDeleted & Restore Tests 

    [Fact]
    public void MarkAsDeleted_ShouldMarkProductAndAllVariantsAsDeleted()
    {
        var product = GetActive();
        product.AddVariant("SKU-1", 100m, 10);
        product.AddVariant("SKU-2", 200m, 20);

        product.MarkAsDeleted();

        product.IsDeleted.Should().BeTrue();
        product.Variants.All(v => v.IsDeleted).Should().BeTrue();
    }

    [Fact]
    public void Restore_ShouldResetProductStatusAndRestoreAllVariants()
    {
        var product = GetActive();
        product.SetPublishStatus(true); 
        product.AddVariant("SKU-1", 100m, 10);

        product.MarkAsDeleted();

        product.Restore();

        product.IsDeleted.Should().BeFalse();
        product.IsPublished.Should().BeFalse();
        product.PublishedAt.Should().BeNull();
        product.Status.Should().Be(ProductStatus.Draft);
        product.Variants.All(v => !v.IsDeleted).Should().BeTrue();
    }

    #endregion
} 
