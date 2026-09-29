using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniMart.Application.Common;
using OmniMart.Application.Interfaces;
using OmniMart.Application.Interfaces.Security;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.Application.Features.Products.Commands;

public record UploadProductImageCommand(
    Guid ProductId,
    Stream FileStream,
    string FileName,
    bool IsPrimary
) : IRequest<Result<bool>>;

public class UploadProductImageCommandHandler : IRequestHandler<UploadProductImageCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UploadProductImageCommandHandler> _logger;

    public UploadProductImageCommandHandler(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        ICurrentUserService currentUserService,
        ILogger<UploadProductImageCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(UploadProductImageCommand request, CancellationToken cancellationToken)
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

        ImageStorageResult uploadResult;
        try
        {
            uploadResult = await _fileStorageService.UploadImageAsync(request.FileStream, request.FileName);
            _logger.LogInformation("Image successfully uploaded to Cloudinary for ProductId {ProductId}.", product.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cloudinary upload failed for ProductId {ProductId}.", product.Id);
            return Result<bool>.Failure("Failed to upload image to the storage server.", ErrorType.Failure);
        }

        try
        {
            product.AddImage(uploadResult.ImageUrl, uploadResult.PublicId, request.IsPrimary);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await _fileStorageService.DeleteImageAsync(uploadResult.PublicId);
            _logger.LogWarning("Rolled back Cloudinary image {PublicId} due to DB failure: {Message}", uploadResult.PublicId, ex.Message);

            return Result<bool>.Failure(ex.Message, ErrorType.Validation);
        }
    }
    public class UploadProductImageCommandValidator : AbstractValidator<UploadProductImageCommand>
    {
        public UploadProductImageCommandValidator()
        {
            RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product ID is required.");
            RuleFor(x => x.FileName).NotEmpty().WithMessage("File name is required.");
            RuleFor(x => x.FileStream).NotNull().WithMessage("File stream cannot be null.");
        }
    }
}