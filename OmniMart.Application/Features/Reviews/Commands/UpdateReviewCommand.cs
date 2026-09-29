using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Reviews.Commands;

public record UpdateReviewCommand(Guid ReviewId, int Rating, string? Comment) : IRequest<Result<bool>>;

public class UpdateReviewCommandHandler : IRequestHandler<UpdateReviewCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateReviewCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public UpdateReviewCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateReviewCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<bool>.Failure("Unauthorized.", ErrorType.Unauthorized);

        var customerId = await _unitOfWork.CustomerProfiles.GetCustomerIdByUserIdAsync(userGuid, cancellationToken);
        if (customerId == null)
            return Result<bool>.Failure("Customer profile not found.", ErrorType.NotFound);

        var review = await _unitOfWork.ProductReviews.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review == null)
            return Result<bool>.Failure("Review not found.", ErrorType.NotFound);

        if (review.CustomerId != customerId.Value)
            return Result<bool>.Failure("You can only update your own reviews.", ErrorType.Unauthorized);

        try
        {
            review.UpdateReview(request.Rating, request.Comment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Review {ReviewId} successfully updated by Customer {CustomerId}.", request.ReviewId, customerId.Value);
            return Result<bool>.Success(true);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Result<bool>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public class UpdateReviewCommandValidator : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty().WithMessage("Review ID is required.");
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");
        RuleFor(x => x.Comment).MaximumLength(1000).WithMessage("Comment cannot exceed 1000 characters.");
    }
}