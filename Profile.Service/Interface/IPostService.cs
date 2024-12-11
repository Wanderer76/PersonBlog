using Profile.Service.Models.File;
using Profile.Service.Models.Post;

namespace Profile.Service.Interface
{
    public interface IPostService
    {
        Task<Guid> CreatePost(PostCreateDto postCreateDto);
        /// <summary>
        /// </summary>
        /// <param name="postId"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="output"></param>
        /// <returns>Возвращает id файла</returns>
        Task<Guid> GetVideoChunkStreamByPostIdAsync(Guid postId, long offset, long length, Stream output);
        Task<FileMetadataModel> GetVideoFileMetadataByPostIdAsync(Guid fileId);
        Task GetVideoStream(Guid postId, Stream output);
        ValueTask<bool> HasVideoExistByPostIdAsync(Guid postId);

        Task<PostPagedListViewModel> GetPostsByBlogIdPagedAsync(Guid blogId, int page, int limit);
    }

}
