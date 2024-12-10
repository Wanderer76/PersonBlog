using Profile.Service.Models.Post;

namespace Profile.Service.Interface
{
    public interface IPostService
    {
        Task<Guid> CreatePost(PostCreateDto postCreateDto);
    }
}
