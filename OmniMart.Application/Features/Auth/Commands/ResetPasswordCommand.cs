using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using OmniMart.Application.Interfaces.Security;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Auth.Command;

public record ResetPasswordCommand(string Email, string Token, string NewPassword, string ConfirmPassword) : IRequest<Result<bool>>;


public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<bool>>
{
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(IPasswordHasherService passwordHasher, IUnitOfWork unitOfWork, IDistributedCache cache, ILogger<ResetPasswordCommandHandler> logger)
    {
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _cache = cache; _logger = logger;
    }

    public async Task<Result<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null) return Result<bool>.Failure("Invalid request.", ErrorType.Failure);

        string? savedOtp = await _cache.GetStringAsync($"ResetToken_{request.Email}", cancellationToken);

        if (string.IsNullOrEmpty(savedOtp) || savedOtp != request.Token)
        {
            _logger.LogWarning("Invalid or expired token for user {UserId}", user.Id);
            return Result<bool>.Failure("The verification code is incorrect or expired.", ErrorType.Validation);
        }

        await _cache.RemoveAsync($"ResetToken_{request.Email}", cancellationToken);

        return Result<bool>.Success(true);
    }
}
public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty().Length(6).WithMessage("The verification code must consist of 6 digits.");
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6).WithMessage("The password is too short.");
        RuleFor(x => x.ConfirmPassword).Equal(x => x.NewPassword).WithMessage("The passwords do not match.");
    }
}
