using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using OmniMart.Application.Interfaces;
using OmniMart.Infrastructure.Services.Settings;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OmniMart.Infrastructure.Services.Settings;

public class CloudinaryService : IFileStorageService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IOptions<CloudinarySettings> config)
    {
        var account = new Account(
            config.Value.CloudName,
            config.Value.ApiKey,
            config.Value.ApiSecret
        );
        _cloudinary = new Cloudinary(account);
    }

    public async Task<ImageStorageResult> UploadImageAsync(Stream fileStream, string fileName)
    {
        if (fileStream == null || fileStream.Length == 0)
            throw new ArgumentException("The uploaded file is empty.");

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Transformation = new Transformation().Height(800).Width(800).Crop("fill")
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            throw new Exception($"Cloudinary Error: {uploadResult.Error.Message}");
        }

        return new ImageStorageResult(uploadResult.SecureUrl.ToString(), uploadResult.PublicId);
    }

    public async Task<bool> DeleteImageAsync(string publicId)
    {
        var deleteParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deleteParams);

        return result.Result == "ok";
    }
}