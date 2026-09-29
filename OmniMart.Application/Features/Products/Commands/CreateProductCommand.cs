using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;

namespace OmniMart.Application.Features.Products.Commands;

public record VariantRequestDto(
     string SKU,
     decimal Price,
     int StockQuantity
 );

public record CreateProductCommand(
    Guid CategoryId,
    string Name,
    string Description,
    decimal BasePrice,
    List<VariantRequestDto> Variants
) : IRequest<Result<Guid>>;
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    ICurrentUserService _currentUserService;
    private readonly ILogger<CreateProductCommandHandler> _logger;
    public CreateProductCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<CreateProductCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to create product '{ProductName}' for CategoryId: {CategoryId}", request.Name, request.CategoryId);
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
        {
            return Result<Guid>.Failure("Unauthorized: Invalid user token.", ErrorType.Unauthorized);
        }

        Guid? vendorId = await _unitOfWork.VendorProfiles.GetVendorIdByUserIdAsync(userGuid, cancellationToken);
        if (vendorId is null)
        {
            return Result<Guid>.Failure("Vendor profile not found.", ErrorType.NotFound);
        }
        bool categoryExists = await _unitOfWork.Categories.IsCategoryExistsAsync(request.CategoryId, cancellationToken);
        if (!categoryExists) {
            _logger.LogWarning("Product creation failed. Category ID {CategoryId} was not found.", request.CategoryId);
            return Result<Guid>.Failure("Category not found.", ErrorType.NotFound);
        }
        

        var newProduct = new Product(
            vendorId: vendorId.Value,
            categoryId: request.CategoryId,
            name: request.Name,
            description: request.Description,

            basePrice: request.BasePrice
        );
        var requestedSkus = request.Variants.Select(v => v.SKU).ToList();
        var existingSkus = await _unitOfWork.Products.GetExistingSkusAsync(requestedSkus, cancellationToken);
        
        foreach (var variantRequest in request.Variants)
        {
            try
            {
                if (existingSkus.Contains(variantRequest.SKU))
                {
                    return Result<Guid>.Failure($"SKU '{variantRequest.SKU}' already exists.", ErrorType.Conflict);
                }
                newProduct.AddVariant(variantRequest.SKU, variantRequest.Price, variantRequest.StockQuantity);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Failed to add variant SKU: {SKU} to product '{ProductName}'. Reason: {Message}", variantRequest.SKU, request.Name, ex.Message);
                return Result<Guid>.Failure(ex.Message);
            }
        }
        await _unitOfWork.Products.AddAsync(newProduct, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully created product '{ProductName}' with ID: {ProductId}. Total Variants: {VariantCount}, VendorId: {VendorId}",
            newProduct.Name, newProduct.Id, request.Variants.Count, vendorId.Value);

        return Result<Guid>.Success(newProduct.Id);
    }
}
public class VariantRequestDtoValidator : AbstractValidator<VariantRequestDto>
{
    public VariantRequestDtoValidator()
    {
        RuleFor(x => x.SKU).NotEmpty().WithMessage("SKU cannot be empty.");
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be > 0.");
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0).WithMessage("Stock cannot be negative.");
    }
}
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(v => v.Variants).NotEmpty().WithMessage("At least one variant is required.");
        RuleForEach(x => x.Variants).SetValidator(new VariantRequestDtoValidator());

        RuleFor(x => x.Variants)
            .Must(v => v.Select(x => x.SKU).Distinct().Count() == v.Count)
            .WithMessage("Duplicate SKUs are not allowed in the same request.");

        RuleFor(x => x.Name).NotEmpty().WithMessage("Product name cannot be empty.");
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Category ID cannot be empty.");
        RuleFor(x => x.Description).NotEmpty().WithMessage("Product description cannot be empty.");
        RuleFor(x => x.BasePrice).GreaterThan(0).WithMessage("Base price must be greater than zero.");
    }
}