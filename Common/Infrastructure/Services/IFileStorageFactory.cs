namespace Infrastructure.Services
{
    public interface IFileStorageFactory
    {
        IFileStorage CreateFileStorage();
    }
}
