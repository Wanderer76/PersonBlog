using Blog.Service.Models;
using Blog.Service.Models.File;
using Blog.Service.Models.Post;
using Microsoft.EntityFrameworkCore;

namespace Blog.Service.Services
{
    //TODO добавить кеширование
    public interface IPostService
    {
        Task<Guid> CreatePostAsync(PostCreateDto postCreateDto);
        /// <summary>
        /// </summary>
        /// <param name="postId"></param>
        /// <param name="fileMetadataId"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns>Возвращает id файла</returns>
        /// <param name="output"></param>
        [Obsolete("Пригоден для .mp4 сейчас не поддерживается")]
        Task<Guid> GetVideoChunkStreamByPostIdAsync(Guid postId, Guid fileMetadataId, long offset, long length, Stream output);
        Task<FileMetadataModel> GetVideoFileMetadataByPostIdAsync(Guid fileId);
        ValueTask<bool> HasVideoExistByPostIdAsync(Guid postId);
        Task<PostPagedListViewModel> GetPostsByBlogIdPagedAsync(Guid blogId, int page, int limit);
        Task RemovePostByIdAsync(Guid id);
        Task UploadVideoChunkAsync(UploadVideoChunkDto uploadVideoChunkDto);
        Task<PostModel> UpdatePostAsync(PostEditDto postEditDto);
        Task<PostDetailViewModel> GetDetailPostByIdAsync(Guid postId);
        Task SetVideoViewed(ViewedVideoModel value);
        Task SetReactionToPost(ReactionCreateModel value);
        Task<bool> CheckForViewAsync(Guid? userId, string? ipAddress);
    }
}
