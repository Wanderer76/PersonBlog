using Infrastructure.Models;

namespace FileStorage.Service;

public interface IImageConvertService
{
    Task<Result<FileMetadataModel>> ConvertImageToPngAsync(FileMetadataModel image);
}