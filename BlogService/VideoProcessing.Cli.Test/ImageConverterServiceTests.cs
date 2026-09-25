using System.Net;
using Infrastructure.Models;
using MediaProcessing.Contract;

namespace VideoProcessing.Cli.Test;

public sealed class ImageConverterServiceTests
{
    [Fact]
    public async Task ConvertImageToPngAsync_UsesOriginalFileNameInMultipartContent()
    {
        var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://converter/")
        };
        var service = new ImageConverterService(httpClient);
        var sourceBytes = "jpeg-content"u8.ToArray();
        var source = new FileMetadataModel
        {
            Name = "InlineMedia",
            FileName = "photo_2026-05-31_16-32-27.jpg",
            FileExtension = ".jpg",
            ContentType = "image/jpeg",
            Length = sourceBytes.Length,
            ContentStream = new MemoryStream(sourceBytes)
        };

        var result = await service.ConvertImageToPngAsync(source);

        Assert.True(result.IsSuccess);
        Assert.Contains(
            "photo_2026-05-31_16-32-27.jpg",
            handler.RequestBody,
            StringComparison.Ordinal);
        Assert.DoesNotContain("filename=InlineMedia", handler.RequestBody, StringComparison.Ordinal);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent("png-content"u8.ToArray())
            };
        }
    }
}
