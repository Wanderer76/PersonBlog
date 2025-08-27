using Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public interface IAudioExtractorService
    {
        Task<AudioFileMetadata> ExtractMetadataAsync(IFormFile mp3File);
        Task<string> ExtractCoverToBase64Async(string filePath);
        Task<bool> ValidateMp3FileAsync(string filePath);
    }
}
