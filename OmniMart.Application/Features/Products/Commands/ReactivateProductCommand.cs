using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Enums;

namespace OmniMart.Application.Features.Products.Commands;

public record ReactivateProductCommand(Guid Id) : IRequest<Result<bool>>;

public class ReactivateProductCommandHandler : IRequestHandler<ReactivateProductCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReactivateProductCommandHandler> _logger;

    public ReactivateProductCommandHandler(IUnitOfWork unitOfWork, ILogger<ReactivateProductCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ReactivateProductCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling ReactivateProductCommand for Product ID: {ProductId}", request.Id);

        var product = await _unitOfWork.Products.GetAsync(p => p.Id == request.Id, cancellationToken);

        if (product == null)
        {
            _logger.LogWarning("Product with ID: {ProductId} not found.", request.Id);
            return Result<bool>.Failure("Product not found.", ErrorType.NotFound);
        }

        if (product.Status != ProductStatus.Suspended)
        {
            _logger.LogWarning("Product with ID: {ProductId} is not suspended and cannot be reactivated.", request.Id);
            return Result<bool>.Failure("Only suspended products can be reactivated.", ErrorType.Conflict);
        }

        product.ReactivateProduct();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product with ID: {ProductId} has been reactivated successfully.", request.Id);
        return Result<bool>.Success(true);
    }
}

public class ReactivateProductCommandValidator : AbstractValidator<ReactivateProductCommand>
{
    public ReactivateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Product ID is required.");
    }
}