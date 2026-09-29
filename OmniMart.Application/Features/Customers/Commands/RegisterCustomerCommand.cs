using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;

namespace OmniMart.Application.Features.Customers.Commands;

public record RegisterCustomerCommand(
        string FirstName,
        string LastName,
        string Email,
        string PhoneNumber,
        string Password
    ) : IRequest<Result<Guid>>;

public class RegisterCustomerCommandHandler : IRequestHandler<RegisterCustomerCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ILogger<RegisterCustomerCommandHandler> _logger;
    public RegisterCustomerCommandHandler(IUnitOfWork unitOfWork, IPasswordHasherService passwordHasher, ILogger<RegisterCustomerCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }
    public async Task<Result<Guid>> Handle(RegisterCustomerCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting registration process for new customer. Email: {Email}", request.Email);

        bool isEmailExists = await _unitOfWork.Users.IsEmailExistsAsync(request.Email, cancellationToken);
        if (isEmailExists) {
            _logger.LogWarning("Customer registration failed. Email '{Email}' is already in use.", request.Email);
            return Result<Guid>.Failure("The email already exists.", ErrorType.Conflict);
        }
        bool isPhoneExists = await _unitOfWork.Users.IsPhoneNumberExistsAsync(request.PhoneNumber, cancellationToken);
        if (isPhoneExists) {
            _logger.LogWarning("Customer registration failed. Phone number '{Phone}' is already in use.", request.PhoneNumber);
            return Result<Guid>.Failure("Phone number already exists.", ErrorType.Conflict);
        }
        string randomPart = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        string userNumber = $"ACC_{randomPart}";
        
        var newUser = new User(
            firstName: request.FirstName,
            lastName: request.LastName,
            email: request.Email,
            phoneNumber: request.PhoneNumber!,
            passwordHash: _passwordHasher.HashPassword(request.Password),
            accountNumber : userNumber,
             role: SystemRole.Customer
            );
        await _unitOfWork.Users.AddAsync(newUser);
        var profile = new CustomerProfile(newUser.Id);
        await _unitOfWork.CustomerProfiles.AddAsync(profile);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            return Result<Guid>.Failure("There was a system conflict during registration. Please try again.", ErrorType.Conflict);
        }
        _logger.LogInformation("Successfully registered new customer. UserId: {UserId}, AccountNumber: {AccountNumber}", newUser.Id, userNumber);
        return Result<Guid>.Success(newUser.Id);

    }

}

public class RegisterCustomerCommandValidator : AbstractValidator<RegisterCustomerCommand> 
{
    public RegisterCustomerCommandValidator() 
    { 
    RuleFor(c => c.FirstName).NotEmpty().WithMessage("FirstName cannot be empty.");
    RuleFor(c => c.LastName).NotEmpty().WithMessage("LastName cannot be empty.");
    RuleFor(c => c.Email).NotEmpty().WithMessage("Email cannot be empty.")
        .EmailAddress().WithMessage("Invalid email format.");
    RuleFor(c => c.PhoneNumber).NotEmpty().WithMessage("PhoneNumber cannot be empty.");
    RuleFor(c => c.Password).NotEmpty().WithMessage("Password cannot be empty.");
    }
}