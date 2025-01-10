using FileStorage.Service.Models;

namespace FileStorage.Service.Service
{
    public interface IFileStorage
    {
        Task<string> PutFileAsync(Guid userId, Guid id, Stream input);
        //Task PutFileAsync(Guid userId, Guid id, string resolution, Stream input);
        Task<string> PutFileWithOriginalResolutionAsync(Guid userId, Guid id, Stream input, VideoResolution resolution = VideoResolution.Original);
        Task<string> GetFileUrlAsync(Guid userId, string objectName);
        Task<string> GetUrlToUploadFileAsync(Guid userId, Guid fileId, VideoResolution resolution = VideoResolution.Original);
        
        Task ReadFileAsync(Guid userId, string objectName, Stream output);
        [Obsolete("Хрень какая то")]
        Task<long> ReadFileByChunksAsync(Guid userId, Guid id, long offset, long length, byte[] output);
        Task<long> ReadFileByChunksAsync(Guid userId, string objectName, long offset, long length, Stream output);
       // Task<long> ReadFileByChunksAsync(Guid userId, Guid id, long offset, long length, string resolution, Stream output);
        Task RemoveFileAsync(Guid userId, Guid id);
    }
}
