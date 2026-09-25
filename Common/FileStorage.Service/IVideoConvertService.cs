using FileStorage.Service.Models;
using Shared.Utils;

namespace FileStorage.Service
{
    public interface IVideoConvertService
    {
        Task GeneratePreviewAsync(string input, string outputFilePath);
        Task<VideoMediaInfo?> GetVideoMediaInfoAsync(string input);
        Task CreateHlsAsync(string input, string output, HlsOptions options, AsyncProgress<double>? action = null);
    }
}
