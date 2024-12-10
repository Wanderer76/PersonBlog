using FileStorage.Service;
using FileStorage.Service.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Profile.Domain.Entities;
using Profile.Service.Models.Post;
using Shared.Persistence;
using Shared.Services;

namespace Profile.Service.Interface.Implementation
{
    internal class DefaultPostService : IPostService
    {
        private readonly IReadWriteRepository<IProfileEntity> _context;
        private readonly IFileStorageFactory _fileStorageFactory;

        public DefaultPostService(IReadWriteRepository<IProfileEntity> context, IConfiguration configuration, IFileStorageFactory fileStorageFactory)
        {
            _context = context;
            _fileStorageFactory = fileStorageFactory;
        }

        public async Task<Guid> CreatePost(PostCreateDto postCreateDto)
        {
            var userProfileId = await _context.Get<AppProfile>()
                .Where(x => x.UserId == postCreateDto.UserId)
                .Select(x => x.Id)
                .FirstAsync();

            var blog = await _context.Get<Blog>()
            .FirstAsync(x => x.ProfileId == userProfileId);

            var postId = GuidService.GetNewGuid();
            Guid? fileId = null;
            var now = DateTimeOffset.UtcNow;

            if (postCreateDto.Type == PostType.Media)
            {
                var storage = _fileStorageFactory.CreateFileStorage();
                var video = postCreateDto.Video!;
                fileId = GuidService.GetNewGuid();
                await storage.PutFileAsync(userProfileId, fileId!.Value, video.OpenReadStream());

                var videoMetadata = new VideoMetadata
                {
                    Id = GuidService.GetNewGuid(),
                    ContentType = video.ContentType,
                    CreatedAt = now,
                    Length = video.Length,
                    Name = video.Name,
                    FileId = fileId.Value
                };
                _context.Add(videoMetadata);
            }


            var post = new Post(postId, blog.Id, postCreateDto.Type, now, postCreateDto.Text, fileId, false, postCreateDto.Title);
            _context.Add(post);
            await _context.SaveChangesAsync();
            return postId;
        }
    }
}
