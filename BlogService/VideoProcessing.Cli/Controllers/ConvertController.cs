using FFmpeg.Service;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace VideoProcessing.Cli.Controllers;

public class ConvertController(ILogger<BaseApiController> logger, IImageConvertService imageConvertService) : BaseApiController(logger)
{
    [HttpPost("convertToPng")]
    public async Task<IActionResult> ConvertImageToPng([FromForm] Files image)
    {
        var file = image.Image.ConvertToFileMetadata();
        var convertedFile = await imageConvertService.ConvertImageToPngAsync(file);

        if (convertedFile.IsFailure)
            return BadRequest(convertedFile.Errors);

        return File(convertedFile.Value.ContentStream, "image/png", convertedFile.Value.FileName);
    }
}

public class Files
{
    public IFormFile Image { get; set; }
}
