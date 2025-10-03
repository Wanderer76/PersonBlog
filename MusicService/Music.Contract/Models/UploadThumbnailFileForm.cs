using Microsoft.AspNetCore.Http;

namespace Music.Contract.Models;

public class UploadThumbnailFileForm
{
    public IFormFile Thumbnail { get; set; }
}
