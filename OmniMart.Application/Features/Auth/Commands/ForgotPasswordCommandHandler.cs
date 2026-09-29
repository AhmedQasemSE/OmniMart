using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Domain.Events;

namespace OmniMart.Application.Features.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest<Result<bool>>;


public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(IUnitOfWork unitOfWork, IMediator mediator, IDistributedCache cache, ILogger<ForgotPasswordCommandHandler> logger)
    {
        _unitOfWork = unitOfWork; _mediator = mediator; _cache = cache; _logger = logger;
    }

    public async Task<Result<bool>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Password reset requested for non-existing email: {Email}", request.Email);
            return Result<bool>.Success(true);
        }

        string otpToken = Random.Shared.Next(100000, 999999).ToString();
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) };
        await _cache.SetStringAsync($"ResetToken_{user.Email}", otpToken, options, cancellationToken);

        await _mediator.Publish(new PasswordResetRequestedEvent(user.Id, otpToken), cancellationToken);
        _logger.LogInformation("Password reset event published successfully for user ID: {UserId}.", user.Id);

        return Result<bool>.Success(true);
    }
}
public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email address must be entered.");
    }
}
