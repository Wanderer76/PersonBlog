using Microsoft.AspNetCore.Http;

namespace Music.Contract.Models;

public class UploadFileForm
{
    public double Duration { get; set; }
    public IFormFile Track { get; set; }
}
