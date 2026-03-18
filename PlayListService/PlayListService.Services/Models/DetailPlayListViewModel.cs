using Blog.Contracts.Models;
using Shared.Models;

namespace PlayListService.Services.Models;

public sealed class DetailPlayListViewModel
{
    public required PlayListListItem Info { get; set; }
    public required PagedListViewModel<PostCommonModel> PostPage { get; set; } 
}
