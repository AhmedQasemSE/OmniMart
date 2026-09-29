using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Products.Commands;

public record SetPrimaryProductImageCommand(Guid ProductId, Guid ImageId) : IRequest<Result<bool>>;

public class SetPrimaryProductImageCommandHandler : IRequestHandler<SetPrimaryProductImageCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SetPrimaryProductImageCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public SetPrimaryProductImageCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<SetPrimaryProductImageCommandHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(SetPrimaryProductImageCommand request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<bool>.Failure("Unauthorized: Invalid user token.", ErrorType.Unauthorized);

        Guid? vendorId = await _unitOfWork.VendorProfiles.GetVendorIdByUserIdAsync(userGuid, cancellationToken);
        if (vendorId is null)
            return Result<bool>.Failure("Vendor profile not found.", ErrorType.NotFound);

        var product = await _unitOfWork.Products.GetAsync(p => p.Id == request.ProductId, cancellationToken, p => p.Images);
        if (product == null)
            return Result<bool>.Failure("Product not found.", ErrorType.NotFound);

        if (product.VendorId != vendorId.Value)
            return Result<bool>.Failure("You do not have permission to modify this product.", ErrorType.Unauthorized);

        try
        {
            product.SetPrimaryImage(request.ImageId);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Image {ImageId} was set as primary for Product {ProductId} by Vendor {VendorId}.",
                request.ImageId, request.ProductId, vendorId.Value);

            return Result<bool>.Success(true);
        }
        catch (InvalidOperationException ex)
        {
            return Result<bool>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}

public class SetPrimaryProductImageCommandValidator : AbstractValidator<SetPrimaryProductImageCommand>
{
    public SetPrimaryProductImageCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product ID is required.");
        RuleFor(x => x.ImageId).NotEmpty().WithMessage("Image ID is required.");
    }
}