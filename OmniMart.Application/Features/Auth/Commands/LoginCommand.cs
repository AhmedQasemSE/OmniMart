using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Enums;

namespace OmniMart.Application.Features.Auth.Command;

public record LoginCommand(string Email, string Password) : IRequest<Result<AuthResponse>>;
public record AuthResponse(string Name, Guid UserId, string Token, string RefreshToken, SystemRole ProfileRole);
public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IJwtProvider _jwtProvider; 
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordHasherService passwordHasher,
        IJwtProvider jwtProvider,
        ILogger<LoginCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
        _logger = logger;
    }
    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !_passwordHasher.VerifyPassword(request.Password,user.PasswordHash)) 
        {
            _logger.LogWarning("Failed login attempt for email: {Email}. Reason: Invalid credentials.", request.Email);
            return Result<AuthResponse>.Failure("Invalid email or password.");
        }
        user.RecordSuccessfulLogin();
        var token = _jwtProvider.GenerateJwtToken(user);

        var refreshToken = _jwtProvider.GenerateRefreshToken();
        user.UpdateRefreshToken(refreshToken, DateTimeOffset.UtcNow.AddDays(7));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User logged in successfully. UserId: {UserId}, Role: {Role}", user.Id, user.Role);

        return Result<AuthResponse>.Success(new AuthResponse(
            $"{user.FirstName} {user.LastName}",
            user.Id,
            token,
            refreshToken,
            user.Role));
    }
   
}

public class LoginCommandValidator : AbstractValidator<LoginCommand> 
{
    public LoginCommandValidator()
    {
        RuleFor(e => e.Email).NotEmpty().WithMessage("Email cannot be empty.")
            .EmailAddress().WithMessage("Invalid email format.");
        RuleFor(p => p.Password).NotEmpty().WithMessage("Password cannot be empty.");
    }
}