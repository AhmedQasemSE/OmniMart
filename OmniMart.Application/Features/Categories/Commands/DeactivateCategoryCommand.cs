using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;

namespace OmniMart.Application.Features.Categories.Commands;

public record DeactivateCategoryCommand(Guid Id) : IRequest<Result<bool>>;

public class DeactivateCategoryCommandHandler : IRequestHandler<DeactivateCategoryCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDistributedCache _cache;

    public DeactivateCategoryCommandHandler(IUnitOfWork unitOfWork, IDistributedCache cache)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<Result<bool>> Handle(DeactivateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(request.Id, cancellationToken);
        if (category == null) return Result<bool>.Failure("Category not found.", ErrorType.NotFound);

        if (!category.IsActive) return Result<bool>.Failure("Category is already inactive.", ErrorType.Validation);

        category.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(CacheKeys.AllCategories, cancellationToken);
        return Result<bool>.Success(true);
    }
    public class DeactivateCategoryCommandValidator : AbstractValidator<DeactivateCategoryCommand>
    {
        public DeactivateCategoryCommandValidator() => RuleFor(x => x.Id).NotEmpty().WithMessage("Category ID is required.");
    }
}