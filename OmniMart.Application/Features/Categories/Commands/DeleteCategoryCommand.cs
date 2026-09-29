using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;

namespace OmniMart.Application.Features.Categories.Commands;

public record DeleteCategoryCommand(Guid Id) : IRequest<Result<bool>>;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCategoryCommandHandler> _logger;
    private readonly IDistributedCache _cache;

    public DeleteCategoryCommandHandler(IUnitOfWork unitOfWork, ILogger<DeleteCategoryCommandHandler> logger, IDistributedCache cache)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cache = cache;
    }

    public async Task<Result<bool>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(request.Id);
        if (category == null)
        {
            return Result<bool>.Failure("Category not found.", ErrorType.NotFound);
        }

        bool hasSubCategories = await _unitOfWork.Categories.AnyAsync(c => c.ParentCategoryId == request.Id, cancellationToken);
        if (hasSubCategories)
        {
            _logger.LogWarning("Attempted to delete category {CategoryId} but it has sub-categories.", request.Id);
            return Result<bool>.Failure("Cannot delete this category because it contains sub-categories. Please delete or move them first.", ErrorType.Conflict);
        }

        bool hasProducts = await _unitOfWork.Products.AnyAsync(p => p.CategoryId == request.Id, cancellationToken);
        if (hasProducts)
        {
            _logger.LogWarning("Attempted to delete category {CategoryId} but it has linked products.", request.Id);
            return Result<bool>.Failure("Cannot delete this category because there are products linked to it.", ErrorType.Conflict);
        }

        category.SoftDelete();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(CacheKeys.AllCategories, cancellationToken);
        _logger.LogInformation("Category {CategoryId} deleted successfully.", request.Id);

        return Result<bool>.Success(true);
    }
    public class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
    {
        public DeleteCategoryCommandValidator() => RuleFor(x => x.Id).NotEmpty().WithMessage("Category ID is required.");
    }
}