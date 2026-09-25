using Infrastructure.Extensions;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Profile.Application.Services;
using Profile.Domain.Entities;
using Shared.Persistence;
using Shared.Services;

namespace Profile.Service;

internal class ProfilePictureStore(
    IReadWriteRepository<IUserEntity> repository,
    IDateTimeManager dateTimeManager,
    IGuidManager guidManager,
    IFileStorageFactory fileStorageFactory) : IProfilePictureStore
{
    private const string PathPrefix = "profilePic";

    public async Task<string> GetPictureUrlAsync(long profileId, CancellationToken cancellationToken = default)
    {
        var profile = await repository.Get<ProfilePictureFile>()
            .Include(x => x.AppProfile)
            .FirstOrDefaultAsync(x => x.ProfileId == profileId, cancellationToken);

        if (profile == null)
            return null;

        using var storage = fileStorageFactory.CreateFileStorage();
        return await storage.GetFileUrlAsync(profile.AppProfile.UserId, profile.ObjectName, cancellationToken);

    }

    public async Task<Result<string>> UpdatePictureAsync(long profileId, FileMetadataModel fileMetadata, CancellationToken cancellationToken = default)
    {
        using var storage = fileStorageFactory.CreateFileStorage();
        var now = dateTimeManager.UtcNow();
        await repository.Get<ProfilePictureFile>()
            .SoftDeleteAsync(now, x => x.ProfileId == profileId, cancellationToken);

        var profile = await repository.Get<AppProfile>()
            .FirstAsync(x => x.Id == profileId, cancellationToken);

        var objectName = await storage.PutFileAsync(profile.UserId, $"{PathPrefix}/{fileMetadata.Name}", fileMetadata.ContentStream, cancellationToken);

        var result = new ProfilePictureFile
        {
            Id = guidManager.GetNewGuid(),
            ProfileId = profileId,
            ContentType = fileMetadata.ContentType,
            CreatedAt = now,
            FileExtension = fileMetadata.FileExtension,
            Length = fileMetadata.Length,
            Name = fileMetadata.Name,
            ObjectName = objectName,
        };
        repository.Add(result);
        await repository.SaveChangesAsync();
        return result.ObjectName;
    }
}