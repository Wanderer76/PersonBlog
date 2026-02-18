namespace Shared.Models;

public class BaseFileMetadataEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string FileExtension { get; set; } = null!;
    public long Length { get; set; }
    public string ContentType { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public string ObjectName { get; set; } = null!;
}
