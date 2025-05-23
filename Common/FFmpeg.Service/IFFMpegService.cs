﻿using FFmpeg.Service.Models;
using Shared.Utils;

namespace FFmpeg.Service
{
    public interface IFFMpegService
    {
        Task GeneratePreview(string input, string outputFilePath);
        Task<FFProbeStream?> GetVideoMediaInfo(string input);
        Task CreateHls(string input, string output, HlsOptions options, AsyncProgress<double>? action = null);
    }
}
