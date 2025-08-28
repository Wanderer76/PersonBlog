using Infrastructure.Services;
using Music.Domain.Entities;
using Music.Domain.Services;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;

namespace Music.Service.Services
{
    internal class DefaultAvatarService : IAvatarService
    {
        private readonly IWriteRepository<IMusicEntity> _repository;
        private readonly IFileStorageFactory _fileStorageFactory;

        public DefaultAvatarService(IWriteRepository<IMusicEntity> repository, IFileStorageFactory fileStorageFactory)
        {
            _repository = repository;
            _fileStorageFactory = fileStorageFactory;
        }

        public async Task<Result<Guid>> UploadAvatarAsync(FileMetadata fileMetadata, Stream stream)
        {
            var avatarMetadata = new AvatarMetadata(
                GuidService.GetNewGuid(),
                fileMetadata.Name,
                Path.GetExtension(fileMetadata.Name),
                fileMetadata.Length,
                fileMetadata.ContentType,
                fileMetadata.ObjectName
                );

            try
            {
                _repository.Add(avatarMetadata);
                using var storage = _fileStorageFactory.CreateFileStorage();
                await storage.PutFileAsync(avatarMetadata.Id, avatarMetadata.ObjectName, stream);
                await _repository.SaveChangesAsync();
                return avatarMetadata.Id;
            }
            catch (Exception ex)
            {
                using var storage = _fileStorageFactory.CreateFileStorage();
                await storage.RemoveFileAsync(avatarMetadata.Id, avatarMetadata.ObjectName);
                return Result<Guid>.Failure(new Error(ex.Message));
            }

        }
    }
}
