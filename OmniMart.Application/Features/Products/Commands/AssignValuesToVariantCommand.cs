using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Products.Commands;

public record VariantAttributeValueDto(Guid AttributeId, string Value);

public record AssignValuesToVariantCommand(
    Guid ProductId,
    string SKU,
    List<VariantAttributeValueDto> Values,
    byte[] RowVersion
) : IRequest<Result<bool>>;

public class AssignValuesToVariantCommandHandler : IRequestHandler<AssignValuesToVariantCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDistributedCache _cache;
    private readonly ILogger<AssignValuesToVariantCommandHandler> _logger;

    public AssignValuesToVariantCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDistributedCache cache,
        ILogger<AssignValuesToVariantCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(AssignValuesToVariantCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Assigning attribute values to Variant SKU: {SKU} for Product ID: {ProductId}", request.SKU, request.ProductId);

        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<bool>.Failure("Unauthorized: Invalid user token.", ErrorType.Unauthorized);

        Guid? vendorId = await _unitOfWork.VendorProfiles.GetVendorIdByUserIdAsync(userGuid, cancellationToken);
        if (vendorId is null)
            return Result<bool>.Failure("Vendor profile not found.", ErrorType.NotFound);

        var product = await _unitOfWork.Products.GetProductWithAllVariantsForUpdateAsync(request.ProductId, cancellationToken);
        if (product == null)
            return Result<bool>.Failure("Product not found.", ErrorType.NotFound);

        if (product.VendorId != vendorId.Value)
            return Result<bool>.Failure("You do not have permission to modify this product.", ErrorType.Unauthorized);

        var variant = product.Variants.FirstOrDefault(v => v.SKU.Equals(request.SKU, StringComparison.OrdinalIgnoreCase));
        if (variant == null || variant.IsDeleted)
            return Result<bool>.Failure("Active variant not found.", ErrorType.NotFound);

        try
        {
            var category = await _unitOfWork.Categories.GetAsync(
                c => c.Id == product.CategoryId,
                cancellationToken,
                c => c.CategoryAttributes);

            if (category == null)
                return Result<bool>.Failure("Associated category not found.", ErrorType.Failure);

            var incomingAttributeIds = request.Values.Select(v => v.AttributeId).ToList();

            var requiredAttributeIds = category.CategoryAttributes
                .Where(ca => ca.IsRequired)
                .Select(ca => ca.ProductAttributeId)
                .ToList();

            var missingRequiredAttributes = requiredAttributeIds.Except(incomingAttributeIds).ToList();
            if (missingRequiredAttributes.Any())
            {
                _logger.LogWarning("Vendor {VendorId} attempted to omit required attributes for Category {CategoryId}.", vendorId.Value, category.Id);
                return Result<bool>.Failure("You must provide values for all required attributes. Required attributes cannot be deleted or omitted.", ErrorType.Validation);
            }

            var allowedAttributeIds = category.CategoryAttributes.Select(ca => ca.ProductAttributeId).ToList();
            if (incomingAttributeIds.Except(allowedAttributeIds).Any())
            {
                return Result<bool>.Failure("Cannot add attributes that do not belong to this product's category.", ErrorType.Validation);
            }

            var attributesToRemove = variant.VariantAttributeValues
                .Where(v => !incomingAttributeIds.Contains(v.ProductAttributeId))
                .ToList();

            foreach (var attr in attributesToRemove)
            {
                bool isRequired = category.CategoryAttributes.Any(ca => ca.ProductAttributeId == attr.ProductAttributeId && ca.IsRequired);
                if (isRequired)
                {
                    return Result<bool>.Failure("Critical Error: Attempted to delete a required attribute.", ErrorType.Validation);
                }

                variant.RemoveAttributeValue(attr.ProductAttributeId);
            }

            foreach (var incomingAttr in request.Values)
            {
                bool isLinkedToCategory = category.CategoryAttributes.Any(ca => ca.ProductAttributeId == incomingAttr.AttributeId);
                if (!isLinkedToCategory)
                {
                    return Result<bool>.Failure("Cannot add attributes that do not belong to this product's category.", ErrorType.Validation);
                }

                var existingAttr = variant.VariantAttributeValues
                    .FirstOrDefault(v => v.ProductAttributeId == incomingAttr.AttributeId);

                if (existingAttr == null)
                {
                    variant.AddAttributeValue(incomingAttr.AttributeId, incomingAttr.Value);
                }
                else if (existingAttr.Value != incomingAttr.Value)
                {
                    existingAttr.UpdateValue(incomingAttr.Value);
                }
            }

            _unitOfWork.Products.SetOriginalRowVersion(product, request.RowVersion);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _cache.RemoveAsync($"Product_Details_{product.Id}", cancellationToken);

            _logger.LogInformation("Successfully synced attributes and values for Variant SKU: {SKU}", request.SKU);
            return Result<bool>.Success(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict for Product {ProductId}.", request.ProductId);
            return Result<bool>.Failure("Data was modified by another user. Please refresh and try again.", ErrorType.Conflict);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed while assigning attributes to Variant {SKU}.", request.SKU);
            return Result<bool>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public class AssignValuesToVariantCommandValidator : AbstractValidator<AssignValuesToVariantCommand>
{
    public AssignValuesToVariantCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product ID is required.");
        RuleFor(x => x.SKU).NotEmpty().WithMessage("SKU is required.");
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("RowVersion is required.");

        RuleFor(x => x.Values).NotNull().WithMessage("Attribute values cannot be null.");

        RuleFor(x => x.Values)
            .Must(list => list.Select(v => v.AttributeId).Distinct().Count() == list.Count)
            .WithMessage("Duplicate attribute IDs are not allowed in the request.")
            .When(x => x.Values != null);
    }
}