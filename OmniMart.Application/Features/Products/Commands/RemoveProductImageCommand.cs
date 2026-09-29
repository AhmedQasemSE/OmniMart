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

namespace OmniMart.Application.Features.Products.Commands;

public record RemoveProductImageCommand(Guid ProductId, Guid ImageId) : IRequest<Result<bool>>;

public class RemoveProductImageCommandHandler : IRequestHandler<RemoveProductImageCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RemoveProductImageCommandHandler> _logger;

    public RemoveProductImageCommandHandler(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        ICurrentUserService currentUserService,
        ILogger<RemoveProductImageCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(RemoveProductImageCommand request, CancellationToken cancellationToken)
    {
        string? userIdString = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
            return Result<bool>.Failure("Unauthorized: Invalid user token.", ErrorType.Unauthorized);

        Guid? vendorId = await _unitOfWork.VendorProfiles.GetVendorIdByUserIdAsync(userGuid, cancellationToken);
        if (vendorId is null) return Result<bool>.Failure("Vendor profile not found.", ErrorType.NotFound);

        var product = await _unitOfWork.Products.GetAsync(p => p.Id == request.ProductId, cancellationToken, p => p.Images);
        if (product == null) return Result<bool>.Failure("Product not found.", ErrorType.NotFound);

        if (product.VendorId != vendorId.Value)
            return Result<bool>.Failure("You do not have permission to modify this product.", ErrorType.Unauthorized);

        var imageToRemove = product.Images.FirstOrDefault(i => i.Id == request.ImageId);
        if (imageToRemove == null) return Result<bool>.Failure("Image not found.", ErrorType.NotFound);

        try
        {
            product.RemoveImage(request.ImageId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _fileStorageService.DeleteImageAsync(imageToRemove.PublicId);

            _logger.LogInformation("Image {ImageId} successfully deleted for Product {ProductId}.", request.ImageId, product.Id);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete image {ImageId} for product {ProductId}.", request.ImageId, request.ProductId);
            return Result<bool>.Failure(ex.Message, ErrorType.Failure);
        }
    }
}

public class RemoveProductImageCommandValidator : AbstractValidator<RemoveProductImageCommand>
{
    public RemoveProductImageCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product ID is required.");
        RuleFor(x => x.ImageId).NotEmpty().WithMessage("Image ID is required.");
    }
}