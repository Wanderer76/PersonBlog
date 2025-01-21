using FileStorage.Service.Models;
using FileStorage.Service.Service;
using Microsoft.EntityFrameworkCore;
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

        public DefaultPostService(IReadWriteRepository<IProfileEntity> context, IFileStorageFactory fileStorageFactory)
        {
            _context = context;
            _fileStorageFactory = fileStorageFactory;
        }

        public async Task<Guid> CreatePostAsync(PostCreateDto postCreateDto)
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

            if (postCreateDto.Video != null)
            {
                var video = postCreateDto.Video!;
                videoId = GuidService.GetNewGuid();

                var objectName = await storage.PutFileWithResolutionAsync(postId, videoId!.Value, video.OpenReadStream());

                var videoMetadata = new VideoMetadata
                {
                    Id = videoId.Value,
                    ContentType = video.ContentType,
                    CreatedAt = now,
                    Length = video.Length,
                    Name = video.Name,
                    PostId = postId,
                    ObjectName = objectName,
                    FileExtension = Path.GetExtension(video.FileName),
                    Resolution = VideoResolution.Original
                };
                var fileUrl = await storage.GetFileUrlAsync(postId, objectName);
                var videoCreateEvent = new VideoUploadEvent
                {
                    Id = GuidService.GetNewGuid(),
                    FileUrl = fileUrl,
                    IsCompleted = false,
                    UserProfileId = userProfileId,
                    ObjectName = objectName,
                    FileId = videoMetadata.Id
                };
                _context.Add(videoCreateEvent);
                _context.Add(videoMetadata);
            }

            //if (postCreateDto.Photos != null && postCreateDto.Photos.Any())
            //{
            //    foreach (var i in postCreateDto.Photos)
            //    {
            //        var photoId = GuidService.GetNewGuid();
            //        await storage.PutFileAsync(userProfileId, videoId!.Value, i.OpenReadStream());

            //        var photoMetadata = new FileMetadata
            //        {
            //            Id = photoId,
            //            ContentType = i.ContentType,
            //            CreatedAt = now,
            //            Length = i.Length,
            //            FileExtension = Path.GetExtension(i.FileName),
            //            Name = i.Name,
            //            PostId = postId
            //        };
            //        _context.Add(photoMetadata);
            //    }
            //}

            var post = new Post(postId, blog.Id, postCreateDto.Type, now, postCreateDto.Text, false, postCreateDto.Title);
            _context.Add(post);
            await _context.SaveChangesAsync();

            return postId;
        }

        public async Task<FileMetadataModel> GetVideoFileMetadataByPostIdAsync(Guid postId, int resolution = 0)
        {
            var fileMetadata = await _context.Get<Post>()
                .Where(x => x.Id == postId)
                .SelectMany(x => x.VideoFiles)
                .Where(x => x.Resolution == (VideoResolution)resolution)
                .FirstAsync();

            return new FileMetadataModel(
                fileMetadata.ContentType,
                fileMetadata.Length,
                fileMetadata.Name,
                fileMetadata.CreatedAt,
                fileMetadata.Id,
                fileMetadata.ObjectName);
        }

        public async Task<Guid> GetVideoChunkStreamByPostIdAsync(Guid postId, Guid fileMetadataId, long offset, long length, Stream output)
        {
            var videoData = await _context.Get<VideoMetadata>()
                .Where(x => x.Id == fileMetadataId)
                .FirstAsync();
            var storage = _fileStorageFactory.CreateFileStorage();
            await storage.ReadFileByChunksAsync(postId, videoData.ObjectName, offset, length, output);
            return fileMetadataId;
        }

        public Task GetVideoStream(Guid postId, Stream output)
        {
            throw new NotImplementedException();
        }

        public async ValueTask<bool> HasVideoExistByPostIdAsync(Guid postId)
        {
            var hasVideo = await _context.Get<VideoMetadata>()
                .Where(x => x.PostId == postId)
                .AnyAsync();
            return hasVideo;
        }

        public async Task<PostPagedListViewModel> GetPostsByBlogIdPagedAsync(Guid blogId, int page, int limit)
        {
            var pagedPosts = await _context.GetPostByBlogIdPagedAsync(blogId, page, limit);
            var profileId = await _context.Get<Blog>()
                .Where(x => x.Id == blogId)
                .Select(x => x.ProfileId)
                .FirstAsync();
            var fileStorage = _fileStorageFactory.CreateFileStorage();
            var posts = new List<PostModel>(pagedPosts.Posts.Count());
            foreach (var x in pagedPosts.Posts)
            {
                var previewUrl = string.IsNullOrWhiteSpace(x.PreviewId) ? null : await fileStorage.GetFileUrlAsync(x.Id, x.PreviewId);
                var isProcessed = x.VideoFiles.Count != 0 ? x.VideoFiles.Any(a => a.IsProcessed) : false;
                posts.Add(new PostModel(
                                x.Id,
                                x.Type,
                                x.Title,
                                x.Description,
                                x.CreatedAt,
                                previewUrl,
                                x.VideoFiles.Count != 0 && !isProcessed ?
                                new VideoMetadataModel(
                                    x.VideoFiles.FirstOrDefault().Id,
                                    x.VideoFiles.FirstOrDefault().Length,
                                    x.VideoFiles.FirstOrDefault().ContentType,
                                    x.VideoFiles
                                    .Select(x => (int)x.Resolution)
                                    // .Where(x => x != 0)
                                    .OrderBy(x => x),
                                    x.VideoFiles.FirstOrDefault().ObjectName
                                ) : null,
                                isProcessed
                            ));
            }

            return new PostPagedListViewModel
            {
                TotalPageCount = pagedPosts.TotalPagesCount,
                Posts = posts
            };
        }

        public async Task RemovePostByIdAsync(Guid id)
        {
            var post = await _context.Get<Post>()
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();
            if (post != null)
            {
                _context.Attach(post);
                post.IsDeleted = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task UploadVideoChunkAsync(UploadVideoChunkDto uploadVideoChunkDto)
        {
            var fileStorage = _fileStorageFactory.CreateFileStorage();
            var metadata = await _context.Get<VideoMetadata>()
                .Where(x => x.IsProcessed == true)
                .FirstAsync(x => x.PostId == uploadVideoChunkDto.PostId);

            await fileStorage.PutFileChunkAsync(uploadVideoChunkDto.PostId, GuidService.GetNewGuid(), uploadVideoChunkDto.ChunkData, new VideoChunkUploadingInfo
            {
                FileId = metadata.Id,
                ChunkNumber = uploadVideoChunkDto.ChunkNumber,
            });

            if (uploadVideoChunkDto.TotalChunkCount == uploadVideoChunkDto.ChunkNumber)
            {
                _context.Add(new CombineFileChunksEvent
                {
                    Id = GuidService.GetNewGuid(),
                    VideoMetadataId = metadata.Id,
                    IsCompleted = false,
                    CreatedAt = DateTimeOffset.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }
    }
}
