using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Reviews.Commands;

public record DeleteReviewCommand(Guid ReviewId) : IRequest<Result<bool>>;

public class DeleteReviewCommandHandler : IRequestHandler<DeleteReviewCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteReviewCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public DeleteReviewCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteReviewCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<bool>.Failure("Unauthorized.", ErrorType.Unauthorized);

        var review = await _unitOfWork.ProductReviews.GetByIdAsync(request.ReviewId, cancellationToken);
        if (review == null)
            return Result<bool>.Failure("Review not found.", ErrorType.NotFound);

        string role = _currentUserService.Role ?? "";

        if (role != SystemRole.Admin.ToString())
        {
            var customerId = await _unitOfWork.CustomerProfiles.GetCustomerIdByUserIdAsync(userGuid, cancellationToken);
            if (customerId == null || review.CustomerId != customerId.Value)
            {
                _logger.LogWarning("Unauthorized attempt to delete review {ReviewId} by user {UserId}.", request.ReviewId, userGuid);
                return Result<bool>.Failure("You do not have permission to delete this review.", ErrorType.Unauthorized);
            }
        }

        _unitOfWork.ProductReviews.Delete(review);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Review {ReviewId} successfully deleted by user {UserId} (Role: {Role}).", request.ReviewId, userGuid, role);
        return Result<bool>.Success(true);
    }
}

public class DeleteReviewCommandValidator : AbstractValidator<DeleteReviewCommand>
{
    public DeleteReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty().WithMessage("Review ID is required.");
    }
}