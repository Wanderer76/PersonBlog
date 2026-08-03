using FileStorage.Service;
using Infrastructure.Models;
using System.Net.Http.Headers;

namespace MediaProcessing.Contract;

public sealed class ImageConverterService(HttpClient httpClient) : IImageConvertService
{
    public async Task<Result<FileMetadataModel>> ConvertImageToPngAsync(FileMetadataModel model)
    {
        if (model.FileExtension.Contains("png"))
            return model;

        var streamContent = new StreamContent(model.ContentStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        var multipart = new MultipartFormDataContent { { streamContent, "image", model.Name } };
        using var response = await httpClient.PostAsync("Convert/convertToPng", multipart);

        if (!response.IsSuccessStatusCode)
            return Result<FileMetadataModel>.Failure(new Shared.Utils.Error(model.FileName, await response.Content.ReadAsStringAsync()));

        var resultName = Path.GetFileNameWithoutExtension(model.FileName) + ".png";
        var bytes = await response.Content.ReadAsByteArrayAsync();
        return new FileMetadataModel
        {
            ContentStream = new MemoryStream(bytes),
            ContentType = "image/png",
            FileExtension = ".png",
            FileName = resultName,
            Length = bytes.Length,
            Name = model.Name,
        };
    }
}
