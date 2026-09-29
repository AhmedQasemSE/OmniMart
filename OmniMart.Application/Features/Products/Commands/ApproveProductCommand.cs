using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Enums;
using System;

namespace OmniMart.Application.Features.Products.Commands;

public record ApproveProductCommand(Guid ProductId) : IRequest<Result<bool>>;

public class ApproveProductCommandHandler : IRequestHandler<ApproveProductCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApproveProductCommandHandler> _logger;

    public ApproveProductCommandHandler(IUnitOfWork unitOfWork, ILogger<ApproveProductCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    public async Task<Result<bool>> Handle(ApproveProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found.", request.ProductId);
            return Result<bool>.Failure("Product not found.", ErrorType.NotFound);
        }

        if (product.Status == ProductStatus.Active)
        {
            _logger.LogWarning("Product with ID {ProductId} is already active.", request.ProductId);
            return Result<bool>.Failure("Product is already active.", ErrorType.Conflict);
        }

        if (product.Status != ProductStatus.PendingReview)
        {
            _logger.LogWarning("Product {ProductId} cannot be approved from status {Status}.", request.ProductId, product.Status);
            return Result<bool>.Failure("Only pending products can be approved.", ErrorType.Validation);
        }

        product.ApproveProduct();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}

public class ApproveProductCommandValidator : AbstractValidator<ApproveProductCommand>
{
    public ApproveProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("ProductId is required.");
    }
}