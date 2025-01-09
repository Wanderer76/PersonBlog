using Profile.Service.Models.File;
using Profile.Service.Models.Post;

namespace Profile.Service.Interface
{
    //TODO добавить кеширование
    public interface IPostService
    {
        Task<Guid> CreatePost(PostCreateDto postCreateDto);
        /// <summary>
        /// </summary>
        /// <param name="postId"></param>
        /// <param name="fileMetadataId"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns>Возвращает id файла</returns>
        /// <param name="output"></param>
        Task<Guid> GetVideoChunkStreamByPostIdAsync(Guid postId, Guid fileMetadataId, long offset, long length, Stream output);
        Task<FileMetadataModel> GetVideoFileMetadataByPostIdAsync(Guid fileId, int resolution = 0);
        Task GetVideoStream(Guid postId, Stream output);
        ValueTask<bool> HasVideoExistByPostIdAsync(Guid postId);
        Task<PostPagedListViewModel> GetPostsByBlogIdPagedAsync(Guid blogId, int page, int limit);
        Task RemovePostByIdAsync(Guid id);
    }

}
