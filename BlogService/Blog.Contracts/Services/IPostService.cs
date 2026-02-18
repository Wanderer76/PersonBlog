using Blog.Contracts.Models;
using Blog.Contracts.Models.Post;
using Blog.Domain.Entities;
using Shared.Models;
using Shared.Utils;

namespace Blog.Contracts.Services;

//TODO добавить кеширование
public interface IPostService
{
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
    Task RemovePostByIdAsync(Guid id);
    Task<PostDetailViewModel?> GetDetailPostByIdAsync(Guid postId);
    Task SetReactionToPost(ReactionCreateModel value);
    IEnumerable<SelectItem<PostVisibility>> GetPostVisibilityList();
    Task<IReadOnlyList<PostCommonModel>> GetPostCommonModelAsync(IEnumerable<Guid> postIds);
    Task<IReadOnlyList<PostCommonModel>> GetCurrentUserPostListAsync();
}
