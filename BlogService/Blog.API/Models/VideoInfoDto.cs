using Blog.Domain.Entities;
using Blog.Domain.Services.Models;

namespace Blog.API.Models;

public class VideoInfoDto
{
    public ProcessState ProcessState { get; set; }
    public string? PreviewUrl { get; set; }
    public VideoMetadataModel? VideoMetadata { get; set; }
}
