using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Entities;


namespace OmniMart.Application.Features.Categories.Commands;

public record UpdateCategoryCommand
(
    Guid CategoryId,
    Guid? ParentCategoryId,
     string Name,
     string Description,
    byte[] RowVersion
) : IRequest<Result<Guid>>;
public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result<Guid>>
{
    private readonly IDistributedCache _cache;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateCategoryCommandHandler> _logger;
    public UpdateCategoryCommandHandler(IUnitOfWork unitOfWork, ILogger<UpdateCategoryCommandHandler> logger, IDistributedCache cache)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cache = cache;
    }
    public async Task<Result<Guid>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to update category with ID: {CategoryId}. New Name: {Name}, New ParentId: {ParentId}", request.CategoryId, request.Name, request.ParentCategoryId);
        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId,cancellationToken);
        if (category==null)
        {
            _logger.LogWarning("Category update failed. Category with ID {CategoryId} was not found.", request.CategoryId);
            return Result<Guid>.Failure("Category not found.", ErrorType.NotFound);
        }
        if (category.Name != request.Name || category.ParentCategoryId != request.ParentCategoryId)
        {
            bool isCategoryNameExists = await _unitOfWork.Categories.IsCategoryNameExistsUnderParentAsync(request.Name, request.ParentCategoryId, cancellationToken);

            if (isCategoryNameExists)
            {
                _logger.LogWarning("Category update failed. A category with the name '{CategoryName}' already exists under ParentId: {ParentId}", request.Name, request.ParentCategoryId);
                return Result<Guid>.Failure("Category name already exists under the specified parent.", ErrorType.Conflict);
            }
        }
        if (category.ParentCategoryId != request.ParentCategoryId)
        {
            bool ValidParent = await _unitOfWork.Categories.IsValidParentAsync(request.CategoryId, request.ParentCategoryId, cancellationToken);
            if (!ValidParent)
            {
                _logger.LogWarning("Category update failed for ID {CategoryId}. Invalid parent category {ParentId} (circular loop or max depth reached).", request.CategoryId, request.ParentCategoryId);
                return Result<Guid>.Failure("Invalid parent category. Cannot create circular loops or exceed maximum depth of 3 levels.", ErrorType.Failure);
            }
        }
        try { 
        category.UpdateDetails(request.Name, request.Description, request.ParentCategoryId);
            _unitOfWork.Categories.SetOriginalRowVersion(category, request.RowVersion); 
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _cache.RemoveAsync(CacheKeys.AllCategories, cancellationToken);

            _logger.LogInformation("Successfully updated category with ID: {CategoryId}", category.Id);
        return Result<Guid>.Success(category.Id);
         }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict for Category {CategoryId}.", request.CategoryId);
            return Result<Guid>.Failure("Sorry, the status of this request has just been modified by another user. Please refresh the page.", ErrorType.Conflict);
        }
    }
}

public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(i => i.CategoryId).NotEmpty().WithMessage("Category Id is required.");

        RuleFor(n => n.Name)
            .NotEmpty().WithMessage("Name cannot be empty.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");
        RuleFor(d => d.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
            .NotEmpty().WithMessage("Description cannot be empty.");
        RuleFor(x => x.ParentCategoryId)
            .NotEqual(x => x.CategoryId)
            .WithMessage("A category cannot be its own parent.");
    }
}