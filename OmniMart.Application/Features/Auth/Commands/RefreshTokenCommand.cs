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

namespace OmniMart.Application.Features.Auth.Command;

public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthResponse>>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtProvider _jwtProvider;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(IUnitOfWork unitOfWork, IJwtProvider jwtProvider, ILogger<RefreshTokenCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _jwtProvider = jwtProvider;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Invalid refresh token attempt.");
            return Result<AuthResponse>.Failure("Invalid refresh token. Please login again.", ErrorType.Unauthorized);
        }

        if (user.RefreshTokenExpiryTime <= DateTimeOffset.UtcNow)
        {
            _logger.LogWarning("Expired refresh token for user {UserId}.", user.Id);
            return Result<AuthResponse>.Failure("Refresh token has expired. Please log in again.", ErrorType.Unauthorized);
        }

        var newAccessToken = _jwtProvider.GenerateJwtToken(user);
        var newRefreshToken = _jwtProvider.GenerateRefreshToken();

        user.UpdateRefreshToken(newRefreshToken, DateTimeOffset.UtcNow.AddDays(7));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tokens refreshed successfully for user {UserId}.", user.Id);

        return Result<AuthResponse>.Success(new AuthResponse(
            $"{user.FirstName} {user.LastName}",
            user.Id,
            newAccessToken,
            newRefreshToken,
            user.Role));
    }
}

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Refresh token is required.");
    }
}