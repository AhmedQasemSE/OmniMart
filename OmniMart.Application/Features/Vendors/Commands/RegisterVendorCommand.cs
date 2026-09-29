using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;

namespace OmniMart.Application.Features.Vendors.Commands;

public record RegisterVendorCommand
(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Password,
    string StoreName,
    string CommercialRegisterNumber
) : IRequest<Result<Guid>>;

public class RegisterVendorCommandHandler : IRequestHandler<RegisterVendorCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ILogger<RegisterVendorCommandHandler> _logger;

    public RegisterVendorCommandHandler(IUnitOfWork unitOfWork, IPasswordHasherService passwordHasherService, ILogger<RegisterVendorCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasherService;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(RegisterVendorCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting registration process for new vendor. StoreName: {StoreName}, Email: {Email}", request.StoreName, request.Email);

        bool isCommercialRegisterNumberExists = await _unitOfWork.VendorProfiles.IsCommercialRegisterNumberExistsAsync(request.CommercialRegisterNumber, cancellationToken);
        if (isCommercialRegisterNumberExists) {
            _logger.LogWarning("Vendor registration failed. Commercial Register Number '{CRNumber}' already exists.", request.CommercialRegisterNumber);
            return Result<Guid>.Failure("Commercial register number already exists.", ErrorType.Conflict);
        }

        bool isEmailExists = await _unitOfWork.Users.IsEmailExistsAsync(request.Email, cancellationToken);
        if (isEmailExists) {
            _logger.LogWarning("Vendor registration failed. Email '{Email}' is already in use.", request.Email);
            return Result<Guid>.Failure("The email already exists.", ErrorType.Conflict);
        }

        bool isPhoneExists = await _unitOfWork.Users.IsPhoneNumberExistsAsync(request.PhoneNumber, cancellationToken);
        if (isPhoneExists) {
            _logger.LogWarning("Vendor registration failed. Phone number '{Phone}' is already in use.", request.PhoneNumber);
            return Result<Guid>.Failure("Phone number already exists.", ErrorType.Conflict);
        }
        string randomPart = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        string accountNumber = $"ACC_{randomPart}";
        string vendorNumber = $"VND_{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";


        var newUser = new User
        (
            firstName: request.FirstName,
            lastName: request.LastName,
            email: request.Email,
            phoneNumber: request.PhoneNumber,
            passwordHash: _passwordHasher.HashPassword(request.Password),
            accountNumber: accountNumber, 
            role: SystemRole.Vendor
        );
        await _unitOfWork.Users.AddAsync(newUser);

        var newVendor = new VendorProfile
        (
            userId: newUser.Id,
            storeName: request.StoreName,
            commercialRegisterNumber: request.CommercialRegisterNumber,
            commissionRate:5m, 
            vendorNumber: vendorNumber 
        );
        await _unitOfWork.VendorProfiles.AddAsync(newVendor);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            return Result<Guid>.Failure("There was a system conflict during registration. Please try again.", ErrorType.Conflict);
        }

        _logger.LogInformation("Successfully registered new vendor. VendorId: {VendorId}, AccountNumber: {AccountNumber}, VendorNumber: {VendorNumber}", newUser.Id, accountNumber, vendorNumber);

        return Result<Guid>.Success(newUser.Id);
    }

}

public class RegisterVendorCommandValidator : AbstractValidator<RegisterVendorCommand>
{
    public RegisterVendorCommandValidator()
    {
        RuleFor(c => c.FirstName).NotEmpty().WithMessage("FirstName cannot be empty.");
        RuleFor(c => c.LastName).NotEmpty().WithMessage("LastName cannot be empty.");
        RuleFor(c => c.Email).NotEmpty().WithMessage("Email cannot be empty.")
            .EmailAddress().WithMessage("Invalid email format.");
        RuleFor(c => c.PhoneNumber).NotEmpty().WithMessage("PhoneNumber cannot be empty.");
        RuleFor(c => c.Password).NotEmpty().WithMessage("Password cannot be empty.");
        RuleFor(c => c.StoreName).NotEmpty().WithMessage("StoreName cannot be empty.");
        RuleFor(c => c.CommercialRegisterNumber).NotEmpty().WithMessage("CommercialRegisterNumber cannot be empty.");

    }
}