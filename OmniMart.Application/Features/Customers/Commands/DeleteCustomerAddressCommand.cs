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

public record DeleteCustomerAddressCommand(Guid AddressId) : IRequest<Result<bool>>;

public class DeleteCustomerAddressCommandHandler : IRequestHandler<DeleteCustomerAddressCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteCustomerAddressCommandHandler> _logger;
    public DeleteCustomerAddressCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ILogger<DeleteCustomerAddressCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteCustomerAddressCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling DeleteCustomerAddressCommand for AddressId: {AddressId}", request.AddressId);
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
        {
            _logger.LogWarning("Unauthorized access attempt to delete address with AddressId: {AddressId}", request.AddressId);
            return Result<bool>.Failure("Unauthorized.", ErrorType.Unauthorized);
        }
        var customerProfile = await _unitOfWork.CustomerProfiles.GetAsync(
            c => c.UserId == userGuid,
            cancellationToken,
            c => c.Addresses
        );

        if (customerProfile == null)
            return Result<bool>.Failure("Customer profile not found.", ErrorType.NotFound);

        try
        {
            customerProfile.RemoveAddress(request.AddressId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted address with AddressId: {AddressId} for UserId: {UserId}", request.AddressId, userGuid);
            return Result<bool>.Success(true);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error deleting address with AddressId: {AddressId} for UserId: {UserId}", request.AddressId, userGuid);
            return Result<bool>.Failure(ex.Message, ErrorType.NotFound);
        }
    }
}

public class DeleteCustomerAddressCommandValidator : AbstractValidator<DeleteCustomerAddressCommand>
{
    public DeleteCustomerAddressCommandValidator()
    {
        RuleFor(x => x.AddressId).NotEmpty().WithMessage("Address ID is required.");
    }
}