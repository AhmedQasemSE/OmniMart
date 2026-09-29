
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Attributes.Commands;

public record DeleteProductAttributeCommand(Guid Id) : IRequest<Result<bool>>;

public class DeleteProductAttributeCommandHandler : IRequestHandler<DeleteProductAttributeCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteProductAttributeCommandHandler> _logger;

    public DeleteProductAttributeCommandHandler(IUnitOfWork unitOfWork, ILogger<DeleteProductAttributeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteProductAttributeCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to delete Product Attribute ID: {AttributeId}", request.Id);

        var attribute = await _unitOfWork.ProductAttributes.GetByIdAsync(request.Id, cancellationToken);
        if (attribute == null)
        {
            return Result<bool>.Failure("Attribute not found.", ErrorType.NotFound);
        }

        bool isInUse = await _unitOfWork.ProductAttributes.IsAttributeInUseAsync(request.Id, cancellationToken);
        if (isInUse)
        {
            _logger.LogWarning("Cannot delete Attribute ID: {AttributeId}. It is currently in use by categories or variants.", request.Id);
            return Result<bool>.Failure("Cannot delete this attribute because it is linked to categories or products. Please remove those links first.", ErrorType.Conflict);
        }

        _unitOfWork.ProductAttributes.Delete(attribute);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully deleted Product Attribute ID: {AttributeId}", request.Id);

        return Result<bool>.Success(true);
    }
}

public class DeleteProductAttributeCommandValidator : AbstractValidator<DeleteProductAttributeCommand>
{
    public DeleteProductAttributeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Attribute ID is required.");
    }
}