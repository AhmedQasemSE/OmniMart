using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniMart.Application.Features.Products.Commands;

public record SoftDeleteCommand(Guid id) : IRequest<Result<bool>>;

    public class SoftDeleteCommandHandler : IRequestHandler<SoftDeleteCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SoftDeleteCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public SoftDeleteCommandHandler(IUnitOfWork unitOfWork, ILogger<SoftDeleteCommandHandler> logger, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> Handle(SoftDeleteCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling SoftDeleteCommand for Product ID: {ProductId}", request.id);

            string? userIdString = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
                return Result<bool>.Failure("Unauthorized: Invalid user token.", ErrorType.Unauthorized);

            Guid? vendorId = await _unitOfWork.VendorProfiles.GetVendorIdByUserIdAsync(userGuid, cancellationToken);
            if (vendorId is null) return Result<bool>.Failure("Vendor profile not found.", ErrorType.NotFound);

            var product = await _unitOfWork.Products.GetProductWithVariantsAsync(request.id, cancellationToken);
            if (product == null)
            {
                _logger.LogWarning("Product with ID: {ProductId} not found.", request.id);
                return Result<bool>.Failure("Product not found.", ErrorType.NotFound);
            }

            if (product.VendorId != vendorId.Value)
                return Result<bool>.Failure("You do not have permission to delete this product.", ErrorType.Unauthorized);

            product.MarkAsDeleted();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Product with ID: {ProductId} has been soft deleted by vendor.", request.id);
            return Result<bool>.Success(true);
        }
    }
public class SoftDeleteCommandValidator : AbstractValidator<SoftDeleteCommand>
{
    public SoftDeleteCommandValidator()
    {
        RuleFor(x => x.id).NotEmpty().WithMessage("Product ID is required.");
    }
}