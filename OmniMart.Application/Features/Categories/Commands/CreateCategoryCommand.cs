using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
namespace OmniMart.Application.Features.Categories.Commands;

public record CreateCategoryCommand
    (
    string Name,
    string Description,
    Guid? ParentCategoryId
    ) : IRequest<Result<Guid>>;



public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand,Result<Guid>>
{
    private readonly IDistributedCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateCategoryCommandHandler> _logger;

    public CreateCategoryCommandHandler(IUnitOfWork unitOfWork, ILogger<CreateCategoryCommandHandler> logger, IDistributedCache cache)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cache = cache;
    }
    public async Task <Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to create category '{CategoryName}' under ParentId: {ParentId}", request.Name, request.ParentCategoryId);

        bool isCategoryNameExists
            = await _unitOfWork.Categories.IsCategoryNameExistsUnderParentAsync(request.Name, request.ParentCategoryId,cancellationToken);
        if (isCategoryNameExists)
        {
            _logger.LogWarning("Category creation failed. A category with the name '{CategoryName}' already exists under ParentId: {ParentId}", request.Name, request.ParentCategoryId);
            return Result<Guid>.Failure($"Category '{request.Name}' already exists in this level.", ErrorType.Conflict);
        }
        if (request.ParentCategoryId.HasValue) 
        {
            var parentCategory = await _unitOfWork.Categories.GetAsync(
                c=>c.Id == request.ParentCategoryId,cancellationToken
                ,c=>c.ParentCategory!
                );
            if (parentCategory == null)
            {
                return Result<Guid>.Failure("The specified parent category does not exist.", ErrorType.NotFound);
            }
            if (parentCategory.ParentCategoryId != null && parentCategory.ParentCategory?.ParentCategoryId != null) 
            {
                _logger.LogWarning("Category creation failed for '{CategoryName}'. Maximum depth of 3 levels reached.", request.Name);
                return Result<Guid>.Failure("Cannot add category. Maximum depth of 3 levels reached.", ErrorType.Failure);
            }

        }
        var category = new Category(
            request.Name,
            request.ParentCategoryId,
            request.Description
        );
        await _unitOfWork.Categories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(CacheKeys.AllCategories, cancellationToken);
        _logger.LogInformation("Successfully created category '{CategoryName}' with ID: {CategoryId}", request.Name, category.Id);
        return Result<Guid>.Success(category.Id);
    }
}

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(n => n.Name)
            .NotEmpty().WithMessage("Name cannot be empty.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(d => d.Description)
            .NotEmpty().WithMessage("Description cannot be empty.")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
        
    }
}




