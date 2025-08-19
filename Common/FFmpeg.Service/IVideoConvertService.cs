using FFmpeg.Service.Models;
using Shared.Utils;

namespace FFmpeg.Service
{
    public interface IVideoConvertService
    {
        Task GeneratePreviewAsync(string input, string outputFilePath);
        Task<FFProbeStream?> GetVideoMediaInfoAsync(string input);
        Task CreateHlsAsync(string input, string output, HlsOptions options, AsyncProgress<double>? action = null);
    }
}
