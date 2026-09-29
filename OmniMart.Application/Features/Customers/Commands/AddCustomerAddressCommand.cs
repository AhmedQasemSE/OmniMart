using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.CustomerProfiles.Commands;

public record AddCustomerAddressCommand(
    string Title,
    string City,
    string Street,
    string ZipCode,
    string PhoneNumber
) : IRequest<Result<Guid>>;
 
public class AddCustomerAddressCommandHandler : IRequestHandler<AddCustomerAddressCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AddCustomerAddressCommandHandler> _logger;

    public AddCustomerAddressCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<AddCustomerAddressCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(AddCustomerAddressCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling AddCustomerAddressCommand for user {UserId}", _currentUserService.UserId);
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
        {
            _logger.LogWarning("Invalid user ID provided.");
            return Result<Guid>.Failure("Unauthorized.", ErrorType.Unauthorized);
        }
        var customerProfile = await _unitOfWork.CustomerProfiles.GetAsync(
            c => c.UserId == userGuid,
            cancellationToken,
            c => c.Addresses
        );

        if (customerProfile == null)
            return Result<Guid>.Failure("Customer profile not found.", ErrorType.NotFound);

        try
        {
            var newAddressId = customerProfile.AddAddress(
                request.Title,
                request.City,
                request.Street,
                request.ZipCode,
                request.PhoneNumber
            );

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Address added successfully for user {UserId} with address ID {AddressId}", userGuid, newAddressId);
            return Result<Guid>.Success(newAddressId);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Validation error while adding address for user {UserId}", userGuid);
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public class AddCustomerAddressCommandValidator : AbstractValidator<AddCustomerAddressCommand>
{
    public AddCustomerAddressCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required (e.g., Home, Work).")
            .MaximumLength(50);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(250);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
    }
}