using Infrastructure.Models;

namespace FFmpeg.Service;

public interface IImageConvertService
{
    Task<Result<FileMetadataModel>> ConvertImageToPngAsync(FileMetadataModel image);
}