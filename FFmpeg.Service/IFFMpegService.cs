using FFmpeg.Service.Models;

namespace FFmpeg.Service
{
    public interface IFFMpegService
    {
        Task GeneratePreview(string input, string outputFilePath);
        Task<FFProbeStream?> GetVideoMediaInfo(string input);
        Task CreateHls(string input, string output, HlsOptions options,Action<double>?action);
    }
}
