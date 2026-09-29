using System.IO;
using System.Threading.Tasks;

namespace OmniMart.Application.Interfaces;

public record ImageStorageResult(string ImageUrl, string PublicId);

public interface IFileStorageService
{
    Task<ImageStorageResult> UploadImageAsync(Stream fileStream, string fileName);

    Task<bool> DeleteImageAsync(string publicId);
}