using Profile.Service.Models;
using Profile.Service.Models.File;
using Profile.Service.Models.Post;

namespace Profile.Service.Services
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
        Task<FileMetadataModel> GetVideoFileMetadataByPostIdAsync(Guid fileId, int resolution = 0);
        ValueTask<bool> HasVideoExistByPostIdAsync(Guid postId);
        Task<PostPagedListViewModel> GetPostsByBlogIdPagedAsync(Guid blogId, int page, int limit);
        Task RemovePostByIdAsync(Guid id);
        Task UploadVideoChunkAsync(UploadVideoChunkDto uploadVideoChunkDto);
        Task<PostModel> UpdatePostAsync(PostEditDto postEditDto);
        Task<PostDetailViewModel> GetDetailPostByIdAsync(Guid postId);
        Task SetVideoViewed(ViewedVideoModel value);
    }
}
