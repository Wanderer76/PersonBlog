using Profile.Service.Models.Post;

namespace Profile.Service.Interface
{
    public interface IPostService
    {
        Task<Guid> CreatePost(PostCreateDto postCreateDto);
        Task<long> GetVideoStream(Guid postId, long offset, long length, byte[]output);
        Task GetVideoStream(Guid postId, Stream output);
    }
}
