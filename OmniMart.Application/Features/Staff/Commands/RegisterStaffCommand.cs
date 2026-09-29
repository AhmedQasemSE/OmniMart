using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Features.Vendors.Commands;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Entities;
using OmniMart.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniMart.Application.Features.Staff.Commands;
public record RegisterStaffCommand(
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string Password,
    Department DepartmentRole,
    SystemRole Role,
    decimal Salary
) : IRequest<Result<Guid>>;

public class RegisterStaffCommandHandler : IRequestHandler<RegisterStaffCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ILogger<RegisterStaffCommandHandler> _logger;

    public RegisterStaffCommandHandler(IUnitOfWork unitOfWork, IPasswordHasherService passwordHasher, ILogger<RegisterStaffCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }
    public async Task<Result<Guid>> Handle(RegisterStaffCommand request, CancellationToken cancellationToken)
    {
        bool isEmailExists = await _unitOfWork.Users.IsEmailExistsAsync(request.Email, cancellationToken);
        if (isEmailExists)
        {
            _logger.LogWarning("Staff registration failed. Email '{Email}' is already in use.", request.Email);
            return Result<Guid>.Failure("The email already exists.", ErrorType.Conflict);
        }

        bool isPhoneExists = await _unitOfWork.Users.IsPhoneNumberExistsAsync(request.PhoneNumber, cancellationToken);
        if (isPhoneExists)
        {
            _logger.LogWarning("Staff registration failed. Phone number '{Phone}' is already in use.", request.PhoneNumber);
            return Result<Guid>.Failure("Phone number already exists.", ErrorType.Conflict);
        }
        string randomPart = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        string accountNumber = $"ACC_{randomPart}";
        string staffNumber = $"STF_{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";

        var newUser = new User
       (
          firstName: request.FirstName,
          lastName: request.LastName,
          email: request.Email,
          phoneNumber: request.PhoneNumber,
          passwordHash: _passwordHasher.HashPassword(request.Password),
          accountNumber: accountNumber,
          role: request.Role
       );
        var staffProfile = new StaffProfile
        (
            userId: newUser.Id,
            staffNumber: staffNumber,
            departmentRole:  request.DepartmentRole,
            salary: request.Salary
        );

        await _unitOfWork.Users.AddAsync(newUser, cancellationToken);
        await _unitOfWork.StaffProfiles.AddAsync(staffProfile, cancellationToken);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            return Result<Guid>.Failure("There was a system conflict during registration. Please try again.", ErrorType.Conflict);
        }
        _logger.LogInformation("Staff registered successfully with ID '{UserId}' and Staff Number '{StaffNumber}'.", newUser.Id, staffNumber);
        return Result<Guid>.Success(newUser.Id);
    }
}

public class RegisterStaffCommandValidator : AbstractValidator<RegisterStaffCommand>
{
    public RegisterStaffCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required.");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name is required.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email is required.");
        RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("Phone number is required.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
        RuleFor(x => x.Salary).GreaterThan(0).WithMessage("Salary must be greater than zero.");
        RuleFor(x => x.Role)
         .Must(role => role == SystemRole.Admin || role == SystemRole.Manager || role == SystemRole.SupportAgent)
         .WithMessage("Invalid role for staff members. Must be Admin, Manager, or SupportAgent.");
    }
}