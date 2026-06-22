using Microsoft.AspNetCore.Http;

namespace Infrastructure.Models;

public sealed class FileMetadataModel
{
    public required string Name { get; init; } = null!;
    public required string FileName { get; init; } = null!;
    public required string FileExtension { get; init; } = null!;
    public required long Length { get; init; }
    public required string ContentType { get; init; } = null!;
    public required Stream ContentStream {  get; init; } = null!;
}

public static class FileMetadataModelExtensions
{
    public static FileMetadataModel ConvertToFileMetadata(this IFormFile formFile)
    {
        return new FileMetadataModel
        {
            ContentType = formFile.ContentType,
            FileExtension = Path.GetExtension(formFile.FileName),
            Name = formFile.Name,
            Length = formFile.Length,
            FileName = formFile.FileName,
            ContentStream = formFile.OpenReadStream(),
        };
    }
}
