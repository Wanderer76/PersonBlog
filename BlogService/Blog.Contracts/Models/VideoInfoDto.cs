using Blog.Domain.Entities;

namespace Blog.Contracts.Models;

public class VideoInfoDto
{
    public ProcessState ProcessState { get; set; }
    public string? PreviewUrl { get; set; }
    public VideoMetadataModel? VideoMetadata { get; set; }
}
