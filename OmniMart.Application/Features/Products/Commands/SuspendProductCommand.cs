using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Enums;

namespace OmniMart.Application.Features.Products.Commands;

public record SuspendProductCommand(Guid id, string Reason) : IRequest<Result<bool>>;

public class SuspendProductCommandHandler : IRequestHandler<SuspendProductCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SuspendProductCommandHandler> _logger;
    public SuspendProductCommandHandler(IUnitOfWork unitOfWork, ILogger<SuspendProductCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    public async Task<Result<bool>> Handle(SuspendProductCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling SuspendProductCommand for Product ID: {ProductId}", request.id);
        var product = await _unitOfWork.Products.GetAsync(p => p.Id == request.id, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Product with ID: {ProductId} not found.", request.id);
            return Result<bool>.Failure("Product not found.", ErrorType.NotFound);
        }
        if (product.Status == ProductStatus.Suspended)
        {
            _logger.LogWarning("Product with ID: {ProductId} is already suspended.", request.id);
            return Result<bool>.Failure("Product is already suspended.", ErrorType.Conflict);
        }
        if (product.Status != ProductStatus.Active)
        {
            _logger.LogWarning("Product with ID: {ProductId} is not active and cannot be suspended.", request.id);
            return Result<bool>.Failure("Only active products can be suspended.", ErrorType.Conflict);
        }

        product.SuspendProduct(request.Reason);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Product with ID: {ProductId} has been suspended.", request.id);
        return Result<bool>.Success(true);
    }
}
public class SuspendProductCommandValidator : AbstractValidator<SuspendProductCommand>
{
    public SuspendProductCommandValidator()
    {
        RuleFor(x => x.id).NotEmpty().WithMessage("Product ID is required.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Suspension reason is required.");
    }
}