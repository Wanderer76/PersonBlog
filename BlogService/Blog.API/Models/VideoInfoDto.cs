using Blog.Domain.Services.Models;

namespace Blog.API.Models;

public class VideoInfoDto
{
    public bool? ProcessState { get; set; }
    public string? PreviewUrl { get; set; }
    public VideoMetadataModel? VideoMetadata { get; set; }
}
