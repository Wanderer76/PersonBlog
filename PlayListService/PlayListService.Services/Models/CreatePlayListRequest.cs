using Microsoft.AspNetCore.Http;

namespace PlayListService.Services.Models;

public class CreatePlayListRequest
{
    public required string Title { get; set; }
    public IFormFile? Thumbnail { get; set; }
    public List<Guid> PostIds { get; set; } = [];
}
