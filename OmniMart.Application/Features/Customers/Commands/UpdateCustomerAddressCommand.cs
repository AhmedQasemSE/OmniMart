using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.CustomerProfiles.Commands;

public record UpdateCustomerAddressCommand(
    Guid AddressId,
    string Title,
    string City,
    string Street,
    string ZipCode,
    string PhoneNumber
) : IRequest<Result<bool>>;

public class UpdateCustomerAddressCommandHandler : IRequestHandler<UpdateCustomerAddressCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateCustomerAddressCommandHandler> _logger;

    public UpdateCustomerAddressCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<UpdateCustomerAddressCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(UpdateCustomerAddressCommand request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<bool>.Failure("Unauthorized.", ErrorType.Unauthorized);

        var customerProfile = await _unitOfWork.CustomerProfiles.GetAsync(
            c => c.UserId == userGuid,
            cancellationToken,
            c => c.Addresses
        );

        if (customerProfile == null)
            return Result<bool>.Failure("Customer profile not found.", ErrorType.NotFound);

        try
        {
            customerProfile.UpdateAddress(
                request.AddressId,
                request.Title,
                request.City,
                request.Street,
                request.ZipCode,
                request.PhoneNumber
            );

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Customer {CustomerId} successfully updated address {AddressId}.", customerProfile.Id, request.AddressId);

            return Result<bool>.Success(true);
        }
        catch (InvalidOperationException ex)
        {
            return Result<bool>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public class UpdateCustomerAddressCommandValidator : AbstractValidator<UpdateCustomerAddressCommand>
{
    public UpdateCustomerAddressCommandValidator()
    {
        RuleFor(x => x.AddressId).NotEmpty().WithMessage("Address ID is required.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(50);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(250);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
    }
}