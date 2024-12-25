namespace FileStorage.Service.Service
{
    public interface IFileStorage
    {
        Task PutFileAsync(Guid userId, Guid id, Stream input);
        Task PutFileAsync(Guid userId, Guid id, string resolution, Stream input);
        Task ReadFileAsync(Guid userId, Guid id, Stream output);
        [Obsolete("Хрень какая то")]
        Task<long> ReadFileByChunksAsync(Guid userId, Guid id, long offset, long length, byte[] output);
        Task<long> ReadFileByChunksAsync(Guid userId, Guid id, long offset, long length, Stream output);
        Task<long> ReadFileByChunksAsync(Guid userId, Guid id, long offset, long length, string resolution, Stream output);
        Task RemoveFileAsync(Guid userId, Guid id);
    }
}
