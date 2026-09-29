using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniMart.Application.Features.Products.Commands;

public record RejectProductCommand(Guid ProductId, string Reason) : IRequest<Result<bool>>;

public class RejectProductCommandHandler : IRequestHandler<RejectProductCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RejectProductCommandHandler> _logger;
    public RejectProductCommandHandler(IUnitOfWork unitOfWork, ILogger<RejectProductCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    public async Task<Result<bool>> Handle(RejectProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetAsync(p => p.Id == request.ProductId, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found.", request.ProductId);
            return Result<bool>.Failure("Product not found.", ErrorType.NotFound);
        }
        if (product.Status != ProductStatus.PendingReview)
        {
            _logger.LogWarning("Product with ID {ProductId} is not in pending review status.", request.ProductId);
            return Result<bool>.Failure("Only pending products can be rejected.", ErrorType.Validation);
        }
        product.RejectProduct(request.Reason);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Product with ID {ProductId} has been rejected.", request.ProductId);
        return Result<bool>.Success(true);


    }
}

public class RejectProductCommandValidator : AbstractValidator<RejectProductCommand>
{
    public RejectProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("ProductId is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Rejection reason is required.")
            .MinimumLength(10).WithMessage("Please provide a meaningful rejection reason.");
    }
}