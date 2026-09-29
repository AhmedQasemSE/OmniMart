using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using OmniMart.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniMart.Application.Features.Products.Commands;

public record SetPublishStatusCommand(Guid id, bool IsPublished) : IRequest<Result<bool>>;

public class SetPublishStatusCommandHandler : IRequestHandler<SetPublishStatusCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SetPublishStatusCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    public SetPublishStatusCommandHandler(IUnitOfWork unitOfWork, ILogger<SetPublishStatusCommandHandler> logger, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }
    public async Task<Result<bool>> Handle(SetPublishStatusCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting publish status for product with Id {ProductId}.", request.id);
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
        {
            return Result<bool>.Failure("Unauthorized: Invalid user token.", ErrorType.Unauthorized);
        }

        Guid? vendorId = await _unitOfWork.VendorProfiles.GetVendorIdByUserIdAsync(userGuid, cancellationToken);
        if (vendorId is null)
        {
            return Result<bool>.Failure("Vendor profile not found.", ErrorType.NotFound);
        }
        var product = await _unitOfWork.Products.GetAsync(p => p.Id == request.id, cancellationToken);

        if (product == null)
        {
            _logger.LogWarning("Product with Id {ProductId} not found.", request.id);
            return Result<bool>.Failure("Product not found.", ErrorType.NotFound);
        }
        if (product.VendorId != vendorId.Value)
        {
            return Result<bool>.Failure("You do not have permission to modify this product.", ErrorType.Unauthorized);
        }
        if (request.IsPublished && product.Status != ProductStatus.Active)
        {
            _logger.LogWarning("Product with Id {ProductId} is not active. Cannot publish.", request.id);
            return Result<bool>.Failure("Only active products can be published.", ErrorType.Conflict);
        }

        if (product.IsPublished == request.IsPublished)
        {
            _logger.LogInformation("Product publish status is already {IsPublished}.", request.IsPublished);
            return Result<bool>.Success(true);
        }

        product.SetPublishStatus(request.IsPublished);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product publish status changed to {IsPublished}.", request.IsPublished);
        return Result<bool>.Success(true);
    }
}
public class SetPublishStatusCommandValidator : AbstractValidator<SetPublishStatusCommand>
{
    public SetPublishStatusCommandValidator()
    {
        RuleFor(x => x.id).NotEmpty().WithMessage("ProductId is required.");
    }
}