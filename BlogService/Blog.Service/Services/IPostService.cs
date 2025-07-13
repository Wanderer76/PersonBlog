using Blog.Domain.Entities;
using Blog.Domain.Services.Models;
using Blog.Service.Models.File;
using Blog.Service.Models.Post;
using Shared.Models;
using Shared.Utils;

namespace Blog.Service.Services
{
    //TODO добавить кеширование
    public interface IPostService
    {
        Task<Result<Guid, ErrorList>> CreatePostAsync(PostCreateDto postCreateDto);
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
        Task<Result<PostFileMetadataModel, ErrorList>> GetVideoFileMetadataByPostIdAsync(Guid fileId);
        ValueTask<bool> HasVideoExistByPostIdAsync(Guid postId);
        Task<PostPagedListViewModel> GetPostsByBlogIdPagedAsync(Guid blogId, int page, int limit);
        Task RemovePostByIdAsync(Guid id);
        Task<Result<bool>> UploadVideoChunkAsync(UploadVideoChunkDto uploadVideoChunkDto);
        Task<PostModel> UpdatePostAsync(PostEditDto postEditDto);
        Task<PostDetailViewModel> GetDetailPostByIdAsync(Guid postId);
        Task SetVideoViewed(ViewedVideoModel value);
        Task SetReactionToPost(ReactionCreateModel value);
        Task<bool> CheckForViewAsync(Guid? userId, string? ipAddress);
        Task<IEnumerable<SelectItem<PostVisibility>>> GetPostVisibilityListAsync();
        Task<Result<PostEditViewModel>> GetPostUpdateModelAsync(Guid postId);
    }
}
