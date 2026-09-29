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

public record SubmitProductForReviewCommand(Guid Id) : IRequest<Result<bool>>;

public class SubmitProductForReviewHandler : IRequestHandler<SubmitProductForReviewCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SubmitProductForReviewHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    public SubmitProductForReviewHandler(IUnitOfWork unitOfWork, ILogger<SubmitProductForReviewHandler> logger, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }
    public async Task<Result<bool>> Handle(SubmitProductForReviewCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Submitting product with Id {ProductId} for review.", request.Id);
     
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
        {
            _logger.LogWarning("Unauthorized access attempt to submit product for review with ID: {ProductId}. Invalid or missing user token.", request.Id);
            return Result<bool>.Failure("Unauthorized: Invalid user token.", ErrorType.Unauthorized);
        }
        Guid? vendorId = await _unitOfWork.VendorProfiles.GetVendorIdByUserIdAsync(userGuid, cancellationToken);
        if (vendorId is null)
        {
            _logger.LogWarning("Vendor profile not found for user ID: {UserId}. Cannot submit product with ID: {ProductId} for review.", userGuid, request.Id);
            return Result<bool>.Failure("Vendor profile not found.", ErrorType.NotFound);
        }   

        var product = await _unitOfWork.Products.GetAsync(p=>p.Id == request.Id, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Product with Id {ProductId} not found.", request.Id);
            return Result<bool>.Failure("Product not found.", ErrorType.NotFound);
        }
        if (product.VendorId != vendorId.Value)
        {
            _logger.LogWarning("Unauthorized access attempt to submit product for review with ID: {ProductId}. User ID: {UserId}.", request.Id, userGuid);
            return Result<bool>.Failure("You do not have permission to submit this product for review.", ErrorType.Unauthorized);
        }

        if (product.Status != ProductStatus.Draft)
        {
            _logger.LogWarning("Product with Id {ProductId} is not in draft status and cannot be submitted for review.", request.Id);
            return Result<bool>.Failure("Only products in draft status can be submitted for review.", ErrorType.Validation);
        }
        product.SubmitForReview();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Product with Id {ProductId} submitted for review.", request.Id);
        return Result<bool>.Success(true);
    }
}
public class SubmitProductForReviewValidator : AbstractValidator<SubmitProductForReviewCommand>
{
    public SubmitProductForReviewValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("ProductId is required.");
    }
}