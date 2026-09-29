using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Customers.Commands;

public record RedeemLoyaltyPointsCommand(int PointsToRedeem) : IRequest<Result<bool>>;

public class RedeemLoyaltyPointsCommandHandler : IRequestHandler<RedeemLoyaltyPointsCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RedeemLoyaltyPointsCommandHandler> _logger;

    public RedeemLoyaltyPointsCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<RedeemLoyaltyPointsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(RedeemLoyaltyPointsCommand request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<bool>.Failure("Unauthorized.", ErrorType.Unauthorized);

        var profile = await _unitOfWork.CustomerProfiles.GetAsync(c => c.UserId == userGuid, cancellationToken);

        if (profile == null)
            return Result<bool>.Failure("Customer profile not found.", ErrorType.NotFound);

        try
        {
            profile.RedeemLoyaltyPoints(request.PointsToRedeem);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to redeem points for customer {CustomerId}.", profile.Id);
            return Result<bool>.Failure(ex.Message, ErrorType.Conflict);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Customer {CustomerId} successfully redeemed {Points} points.", profile.Id, request.PointsToRedeem);

        return Result<bool>.Success(true);
    }
}

public class RedeemLoyaltyPointsCommandValidator : AbstractValidator<RedeemLoyaltyPointsCommand>
{
    public RedeemLoyaltyPointsCommandValidator()
    {
        RuleFor(x => x.PointsToRedeem)
            .GreaterThan(0).WithMessage("Points to redeem must be greater than zero.");
    }
}