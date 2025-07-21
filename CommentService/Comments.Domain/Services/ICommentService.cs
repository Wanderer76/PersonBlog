using Comments.Domain.Entities;
using Comments.Domain.Models;
using Shared.Utils;

namespace Comments.Domain.Services;

public interface ICommentService
{
    Task<Result<IReadOnlyList<CommentListItem>>> GetCommentsListByPostAsync(Guid postId);
    Task<Result<CommentCreateResponse>> CreateCommentAsync(CommentCreateRequest createRequest);
    Task<Result<CommentCreateResponse>> UpdateCommentAsync(CommentCreateRequest createRequest);
    Task<Result<bool>> RemoveCommentAsync(Guid commentId);
}
