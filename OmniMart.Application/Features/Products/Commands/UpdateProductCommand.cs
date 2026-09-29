using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
namespace OmniMart.Application.Features.Products.Commands;

public record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string Description,
    decimal BasePrice,
    Guid CategoryId,
    byte[] RowVersion
) : IRequest<Result<Guid>>;
public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<Guid>>
{
    private readonly IDistributedCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProductCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public UpdateProductCommandHandler(IUnitOfWork unitOfWork, ILogger<UpdateProductCommandHandler> logger, ICurrentUserService currentUserService, IDistributedCache cache)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
        _cache = cache;
    }
    public async Task<Result<Guid>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
        {
            _logger.LogWarning("Unauthorized access attempt to update product {ProductId}. Invalid user token.", request.ProductId);
            return Result<Guid>.Failure("Unauthorized: Invalid user token.", ErrorType.Unauthorized);
        }
        Guid? vendorId = await _unitOfWork.VendorProfiles.GetVendorIdByUserIdAsync(userGuid, cancellationToken);
        if (vendorId is null)
        {
            _logger.LogWarning("Vendor profile not found for user {UserId}. Cannot update product {ProductId}.", userGuid, request.ProductId);
            return Result<Guid>.Failure("Vendor profile not found.", ErrorType.NotFound);
        }
        var product = await _unitOfWork.Products.GetAsync(p => p.Id == request.ProductId, cancellationToken);
        if (product is null)
        {
            _logger.LogWarning("Product {ProductId} not found for vendor {VendorId}.", request.ProductId, vendorId);
            return Result<Guid>.Failure("Product not found.", ErrorType.NotFound);
        }
        if (product.VendorId != vendorId.Value)
        {
            _logger.LogWarning("User {UserId} attempted to update product {ProductId} which belongs to vendor {VendorId}.", userGuid, request.ProductId, product.VendorId);
            return Result<Guid>.Failure("You do not have permission to update this product.", ErrorType.Unauthorized);
        }
        if (product.CategoryId != request.CategoryId)
        {
            bool categoryExists = await _unitOfWork.Categories.IsCategoryExistsAsync(request.CategoryId, cancellationToken);
            if (!categoryExists)
            {
                _logger.LogWarning("Category {CategoryId} not found while updating product {ProductId}.", request.CategoryId, request.ProductId);
                return Result<Guid>.Failure("Category not found.", ErrorType.NotFound);
            }
        }
        try
        {
            product.UpdateDetails(request.Name, request.Description, request.BasePrice, request.CategoryId);

            _unitOfWork.Products.SetOriginalRowVersion(product, request.RowVersion);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _cache.RemoveAsync($"Product_Details_{request.ProductId}", cancellationToken);

            _logger.LogInformation("Successfully updated product {ProductId} by vendor {VendorId}.", product.Id, vendorId);
            return Result<Guid>.Success(product.Id);
        }
        catch (DbUpdateConcurrencyException) 
        {
            _logger.LogWarning("Concurrency conflict for product {ProductId}.", request.ProductId);
            return Result<Guid>.Failure("Sorry, this product has just been modified by someone else. Please refresh the page for the latest data.", ErrorType.Conflict);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Domain validation failed for product {ProductId}: {Message}", request.ProductId, ex.Message);
            return Result<Guid>.Failure(ex.Message, ErrorType.Failure);
        }
    }
}

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Category ID is required.");
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product ID is required.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Product name is required.");
        RuleFor(x => x.Description).NotEmpty().WithMessage("Product description is required.");
        RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0).WithMessage("Base price cannot be negative.");
    }
}