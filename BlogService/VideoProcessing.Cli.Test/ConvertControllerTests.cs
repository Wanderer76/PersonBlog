using FileStorage.Service;
using Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VideoProcessing.Cli.Controllers;

namespace VideoProcessing.Cli.Test;

public sealed class ConvertControllerTests
{
    [Fact]
    public async Task ConvertImageToPng_WithUnsupportedExtension_ReturnsBadRequestWithoutStartingFfmpeg()
    {
        var converter = new Mock<IImageConvertService>();
        var controller = new ConvertController(NullLogger<BaseApiController>.Instance, converter.Object);
        var content = new MemoryStream("not an image"u8.ToArray());
        var formFile = new FormFile(content, 0, content.Length, "image", "payload.exe");

        var result = await controller.ConvertImageToPng(new Files(formFile), CancellationToken.None);

        Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);
        converter.Verify(
            x => x.ConvertImageToPngAsync(It.IsAny<FileMetadataModel>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ConvertImageToPng_WithEmptyFile_ReturnsBadRequestWithoutStartingFfmpeg()
    {
        var converter = new Mock<IImageConvertService>();
        var controller = new ConvertController(NullLogger<BaseApiController>.Instance, converter.Object);
        var formFile = new FormFile(Stream.Null, 0, 0, "image", "empty.png");

        var result = await controller.ConvertImageToPng(new Files(formFile), CancellationToken.None);

        Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);
        converter.Verify(
            x => x.ConvertImageToPngAsync(It.IsAny<FileMetadataModel>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
