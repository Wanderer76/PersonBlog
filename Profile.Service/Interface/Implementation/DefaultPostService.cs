using FileStorage.Service;
using FileStorage.Service.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Profile.Domain.Entities;
using Profile.Persistence.Repository;
using Profile.Service.Models.File;
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
            Guid? videoId = null;
            var now = DateTimeOffset.UtcNow;
            var storage = _fileStorageFactory.CreateFileStorage();

            if (postCreateDto.Type == PostType.Video)
            {
                var video = postCreateDto.Video!;
                videoId = GuidService.GetNewGuid();
                await storage.PutFileAsync(userProfileId, videoId!.Value, video.OpenReadStream());

                var videoMetadata = new FileMetadata
                {
                    Id = videoId.Value,
                    ContentType = video.ContentType,
                    CreatedAt = now,
                    Length = video.Length,
                    Name = video.Name,
                    PostId = postId
                };
                _context.Add(videoMetadata);
            }

            if (postCreateDto.Photos != null && postCreateDto.Photos.Any())
            {
                foreach (var i in postCreateDto.Photos)
                {
                    var photoId = GuidService.GetNewGuid();
                    await storage.PutFileAsync(userProfileId, videoId!.Value, i.OpenReadStream());

                    var photoMetadata = new FileMetadata
                    {
                        Id = photoId,
                        ContentType = i.ContentType,
                        CreatedAt = now,
                        Length = i.Length,
                        Name = i.Name,
                        PostId = postId
                    };
                    _context.Add(photoMetadata);
                }
            }

            var post = new Post(postId, blog.Id, postCreateDto.Type, now, postCreateDto.Text, videoId, false, postCreateDto.Title);
            _context.Add(post);
            await _context.SaveChangesAsync();
            return postId;
        }

        public async Task<FileMetadataModel> GetVideoFileMetadataByPostIdAsync(Guid postId)
        {
            var fileMetadata = await _context.Get<Post>()
                .Where(x => x.Id == postId)
                .Select(x => x.VideoFile)
                .FirstAsync();

            return new FileMetadataModel(
                fileMetadata.ContentType,
                fileMetadata.Length,
                fileMetadata.Name,
                fileMetadata.CreatedAt);
        }

        public async Task<Guid> GetVideoChunkStreamByPostIdAsync(Guid postId, long offset, long length, Stream output)
        {
            var post = await _context.Get<Post>()
                .Select(x => new
                {
                    x.Id,
                    x.Blog.ProfileId,
                    x.VideoMetadataId
                })
                .FirstAsync(x => x.Id == postId);

            var storage = _fileStorageFactory.CreateFileStorage();
            await storage.ReadFileByChunksAsync(post.ProfileId, post.VideoMetadataId!.Value, offset, length, output);
            return post.VideoMetadataId.Value!;
        }

        public Task GetVideoStream(Guid postId, Stream output)
        {
            throw new NotImplementedException();
        }

        public async ValueTask<bool> HasVideoExistByPostIdAsync(Guid postId)
        {
            var videoId = await _context.Get<Post>()
                .Where(x => x.Id == postId)
                .Select(x => x.VideoMetadataId)
                .FirstOrDefaultAsync();
            return videoId.HasValue;
        }

        public async Task<PostPagedListViewModel> GetPostsByBlogIdPagedAsync(Guid blogId, int page, int limit)
        {
            var pagedPosts = await _context.GetPostByBlogIdPagedAsync(blogId, page, limit);

            var posts = pagedPosts.Posts.Select(x => new PostModel(
                x.Id,
                x.Type,
                x.Title,
                x.Description,
                x.CreatedAt,
                new VideoMetadataModel(
                    x.VideoFile.Id,
                    x.VideoFile.Length,
                    x.VideoFile.ContentType
                )
            ))
                .ToList();

            return new PostPagedListViewModel
            {
                TotalPageCount = pagedPosts.TotalPagesCount,
                Posts = posts
            };
        }

        //Task<Guid> IPostService.GetVideoChunkStreamByPostId(Guid postId, long offset, long length, Stream output)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
