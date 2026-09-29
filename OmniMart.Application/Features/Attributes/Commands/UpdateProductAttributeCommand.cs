using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Attributes.Commands;

public record UpdateProductAttributeCommand(Guid Id, string NewName, byte[] RowVersion) : IRequest<Result<bool>>;

public class UpdateProductAttributeCommandHandler : IRequestHandler<UpdateProductAttributeCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProductAttributeCommandHandler> _logger;
    private readonly IDistributedCache _cache; 

    public UpdateProductAttributeCommandHandler(IUnitOfWork unitOfWork, ILogger<UpdateProductAttributeCommandHandler> logger, IDistributedCache cache)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cache = cache;
    }

    public async Task<Result<bool>> Handle(UpdateProductAttributeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to update Product Attribute ID: {AttributeId}", request.Id);

        var attribute = await _unitOfWork.ProductAttributes.GetByIdAsync(request.Id, cancellationToken);
        if (attribute == null)
        {
            return Result<bool>.Failure("Attribute not found.", ErrorType.NotFound);
        }

        string cleanName = request.NewName.Trim();

        if (!attribute.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase))
        {
            bool isExists = await _unitOfWork.ProductAttributes.IsNameExistsAsync(cleanName, cancellationToken);
            if (isExists)
            {
                return Result<bool>.Failure($"The attribute '{cleanName}' already exists.", ErrorType.Conflict);
            }
        }

        try
        {
            attribute.UpdateName(cleanName);
            _unitOfWork.ProductAttributes.SetOriginalRowVersion(attribute, request.RowVersion);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully updated Product Attribute ID: {AttributeId}", request.Id);

            await _cache.RemoveAsync($"{CacheKeys.ProductAttributesPrefix}1_Size_10", cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<bool>.Failure("Data was modified by another user. Please refresh.", ErrorType.Conflict);
        }
        catch (ArgumentException ex)
        {
            return Result<bool>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public class UpdateProductAttributeCommandValidator : AbstractValidator<UpdateProductAttributeCommand>
{
    public UpdateProductAttributeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Attribute ID is required.");
        RuleFor(x => x.NewName)
            .NotEmpty().WithMessage("Attribute name cannot be empty.")
            .MaximumLength(100).WithMessage("Attribute name cannot exceed 100 characters.");
        RuleFor(x => x.RowVersion).NotEmpty().WithMessage("RowVersion is required.");
    }
}