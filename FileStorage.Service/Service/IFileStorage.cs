namespace FileStorage.Service.Service
{
    public interface IFileStorage
    {
        Task PutFileAsync(Guid userId, Guid id, Stream input);
        Task ReadFileAsync(Guid userId, Guid id, Stream output);
        Task RemoveFileAsync(Guid userId, Guid id);
    }
}
