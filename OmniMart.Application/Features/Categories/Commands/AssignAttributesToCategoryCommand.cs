using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Categories.Commands;

public record CategoryAttributeRequestDto(Guid AttributeId, bool IsRequired);

public record AssignAttributesToCategoryCommand(
    Guid CategoryId,
    List<CategoryAttributeRequestDto> Attributes,
    byte[] RowVersion
) : IRequest<Result<bool>>;

public class AssignAttributesToCategoryCommandHandler : IRequestHandler<AssignAttributesToCategoryCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssignAttributesToCategoryCommandHandler> _logger;
    private readonly IDistributedCache _cache;

    public AssignAttributesToCategoryCommandHandler(IUnitOfWork unitOfWork, ILogger<AssignAttributesToCategoryCommandHandler> logger, IDistributedCache cache)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cache = cache;
    }

    public async Task<Result<bool>> Handle(AssignAttributesToCategoryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Assigning attributes to Category ID: {CategoryId}", request.CategoryId);

        var category = await _unitOfWork.Categories.GetAsync(
            c => c.Id == request.CategoryId,
            cancellationToken,
            c => c.CategoryAttributes
        );

        if (category == null)
        {
            return Result<bool>.Failure("Category not found.", ErrorType.NotFound);
        }

        var incomingAttributeIds = request.Attributes.Select(a => a.AttributeId).ToList();

        var attributesToRemove = category.CategoryAttributes
            .Where(ca => !incomingAttributeIds.Contains(ca.ProductAttributeId))
            .ToList();

        foreach (var attr in attributesToRemove)
        {
            category.RemoveAttribute(attr.ProductAttributeId);
        }

        foreach (var incomingAttr in request.Attributes)
        {
            var existingAttr = category.CategoryAttributes
                .FirstOrDefault(ca => ca.ProductAttributeId == incomingAttr.AttributeId);

            if (existingAttr == null)
            {
                category.AddAttribute(incomingAttr.AttributeId, incomingAttr.IsRequired);
            }
            else if (existingAttr.IsRequired != incomingAttr.IsRequired)
            {
                category.RemoveAttribute(existingAttr.ProductAttributeId);
                category.AddAttribute(incomingAttr.AttributeId, incomingAttr.IsRequired);
            }
        }

        try
        {
            _unitOfWork.Categories.SetOriginalRowVersion(category, request.RowVersion);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _cache.RemoveAsync($"Category_Attributes_{category.Id}", cancellationToken);

            _logger.LogInformation("Successfully updated attributes for Category: {CategoryId}", category.Id);
            return Result<bool>.Success(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict for Category {CategoryId}.", request.CategoryId);
            return Result<bool>.Failure("Data was modified by another user. Please refresh and try again.", ErrorType.Conflict);
        }
        catch (DbUpdateException)
        {
            return Result<bool>.Failure("One or more attributes provided do not exist in the system.", ErrorType.Validation);
        }
        catch (InvalidOperationException ex)
        {
            return Result<bool>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public class AssignAttributesToCategoryCommandValidator : AbstractValidator<AssignAttributesToCategoryCommand>
{
    public AssignAttributesToCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Category ID is required.");
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("RowVersion is required.");
        RuleFor(x => x.Attributes).NotNull().WithMessage("Attributes list cannot be null.");

        RuleFor(x => x.Attributes)
            .Must(list => list.Select(a => a.AttributeId).Distinct().Count() == list.Count)
            .WithMessage("Duplicate attributes are not allowed in the same request.")
            .When(x => x.Attributes != null);
    }
}