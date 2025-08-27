using Microsoft.AspNetCore.Http;

namespace Music.Contract.Models;

public class UploadFileForm
{
    public IFormFile Track { get; set; }
}
