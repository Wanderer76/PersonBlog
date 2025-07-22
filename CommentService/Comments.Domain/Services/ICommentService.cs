using Comments.Domain.Entities;
using Comments.Domain.Models;
using Shared.Utils;

namespace Comments.Domain.Services;

public interface ICommentService
{
    Task<Result<CommentsListViewModel>> GetCommentsListByPostAsync(Guid postId);
    Task<Result<CommentCreateResponse>> CreateCommentAsync(CommentCreateRequest createRequest);
    Task<Result<CommentCreateResponse>> UpdateCommentAsync(CommentCreateRequest createRequest);
    Task<Result> RemoveCommentAsync(Guid commentId);
}
