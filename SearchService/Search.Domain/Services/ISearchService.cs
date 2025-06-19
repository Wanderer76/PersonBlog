using Shared.Utils;

namespace Search.Domain.Services;

public interface ISearchService
{
    Task<Result<IEnumerable<PostModel>>> SearchAsync(SearchOptions query);
    Task<Result<bool>> AddPostAsync(PostModel postModel);
    Task<Result<bool>> UpdatePostAsync(PostModel postModel);
    Task<Result<bool>> RemovePostAsync(Guid id);
}


public class SearchOptions
{
    public string Title { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 10;
}