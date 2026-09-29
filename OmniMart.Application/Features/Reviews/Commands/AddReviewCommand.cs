using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Reviews.Commands;

public record AddReviewCommand(
    Guid ProductId,
    int Rating,
    string? Comment
) : IRequest<Result<Guid>>;

public class AddReviewCommandHandler : IRequestHandler<AddReviewCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddReviewCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public AddReviewCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<AddReviewCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Guid>> Handle(AddReviewCommand request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
        {
            return Result<Guid>.Failure("Unauthorized. Please log in to submit a review.", ErrorType.Unauthorized);
        }

        Guid? customerId = await _unitOfWork.CustomerProfiles.GetCustomerIdByUserIdAsync(userGuid, cancellationToken);
        if (customerId == null)
        {
            return Result<Guid>.Failure("Customer profile not found.", ErrorType.NotFound);
        }

        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
        {
            return Result<Guid>.Failure("Product not found.", ErrorType.NotFound);
        }

        bool hasReviewed = await _unitOfWork.ProductReviews.HasCustomerReviewedProductAsync(request.ProductId, customerId.Value, cancellationToken);
        if (hasReviewed)
        {
            _logger.LogWarning("Customer {CustomerId} attempted to review Product {ProductId} multiple times.", customerId.Value, request.ProductId);
            return Result<Guid>.Failure("You have already reviewed this product.", ErrorType.Conflict);
        }

        try
        {
            Guid reviewId = product.AddReview(customerId.Value, request.Rating, request.Comment);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Review {ReviewId} successfully added for Product {ProductId}.", reviewId, request.ProductId);

            return Result<Guid>.Success(reviewId);
        }
        catch (InvalidOperationException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Failure);
        }
    }
}


public class AddReviewCommandValidator : AbstractValidator<AddReviewCommand>
{
    public AddReviewCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(1000).WithMessage("Comment cannot exceed 1000 characters.");
    }
}