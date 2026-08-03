using FileStorage.Service;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace VideoProcessing.Cli.Controllers;

public class ConvertController(ILogger<BaseApiController> logger, IImageConvertService imageConvertService) : BaseApiController(logger)
{
    private const long MaxImageSize = 20 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmp", ".gif", ".jpeg", ".jpg", ".png", ".tif", ".tiff", ".webp"
    };

    [HttpPost("convertToPng")]
    [RequestSizeLimit(MaxImageSize)]
    public async Task<IActionResult> ConvertImageToPng([FromForm] Files? image, CancellationToken cancellationToken)
    {
        if (image?.Image is null || image.Image.Length == 0)
            return BadRequest("Файл изображения не передан.");

        if (image.Image.Length > MaxImageSize)
            return StatusCode(StatusCodes.Status413PayloadTooLarge);

        var extension = Path.GetExtension(image.Image.FileName);
        if (!AllowedExtensions.Contains(extension))
            return BadRequest("Неподдерживаемый формат изображения.");

        var file = image.Image.ConvertToFileMetadata();
        var convertedFile = await imageConvertService.ConvertImageToPngAsync(file, cancellationToken);

        if (convertedFile.IsFailure)
            return BadRequest(convertedFile.Errors);

        return File(convertedFile.Value.ContentStream, "image/png", convertedFile.Value.FileName);
    }
}

public record Files(IFormFile Image);
