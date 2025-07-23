using Comments.Domain.Entities;
using Comments.Domain.Models;
using Comments.Domain.Services;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;

namespace Comments.Service.Implementation;

internal class DefaultCommentService : ICommentService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IReadWriteRepository<ICommentEntity> _repository;

    public DefaultCommentService(ICurrentUserService currentUserService, IReadWriteRepository<ICommentEntity> repository)
    {
        _currentUserService = currentUserService;
        _repository = repository;
    }

    public async Task<Result<CommentCreateResponse>> CreateCommentAsync(CommentCreateRequest createRequest)
    {
        if (createRequest.ReplyTo.HasValue)
        {
            var isReplyExists = await _repository.Get<Comment>()
                .Where(x => x.Id == createRequest.ReplyTo.Value)
                .AnyAsync();
            if (!isReplyExists)
            {
                return Result<CommentCreateResponse>.Failure(new("Комментария не существует"));
            }
        }

        var user = await _currentUserService.GetCurrentUserAsync();
        var comment = new Comment(user.UserId!.Value, createRequest.PostId, createRequest.Text, createRequest.ReplyTo);
        _repository.Add(comment);
        await _repository.SaveChangesAsync();
        var userEntity = await _repository.Get<UserProfile>()
            .FirstOrDefaultAsync(x => x.UserId == user.UserId.Value);

        return new CommentCreateResponse
        {
            Id = comment.Id,
            UserId = user.UserId.Value,
            PhotoUrl = userEntity?.PhotoUrl,
            ReplyTo = createRequest.ReplyTo,
            Text = createRequest.Text,
            Username = user.UserName,
            CreatedAt = comment.CreatedAt
        };
    }

    public async Task<Result<CommentsListViewModel>> GetCommentsListByPostAsync(Guid postId)
    {
        var comments = await _repository.Get<Comment>()
            .Where(x => x.PostId == postId)
            .ToListAsync();

        var commentsCount = await _repository.Get<Comment>()
            .Where(x => x.PostId == postId)
            .CountAsync();

        var userIds = (await _repository.Get<UserProfile>()
            .Join(_repository.Get<Comment>().Where(x => x.PostId == postId),
            outer => outer.UserId,
            inner => inner.UserId,
            (x, y) => x)
            .Distinct()
            .ToDictionaryAsync(x => x.UserId))!;

        var result = comments.ToTree(c => c.Id, c => c.ParentId)
            .Select(x => MapToCommentListItem(x, (userId) => userIds.TryGetValue(userId, out var user) ? user.Username : null))
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        return new CommentsListViewModel(commentsCount, result);
    }


    public async Task<Result> RemoveCommentAsync(Guid commentId)
    {
        var comment = await _repository.Get<Comment>()
            .FirstOrDefaultAsync(x => x.Id == commentId);

        if(comment == null)
        {
            return Result.Failure(new("Комментария не существует"));
        }

        _repository.Attach(comment);
        comment.Remove();
        await _repository.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<CommentCreateResponse>> UpdateCommentAsync(CommentUpdateRequest createRequest)
    {
        var comment = await _repository.Get<Comment>()
             .FirstOrDefaultAsync(x => x.Id == createRequest.CommentId);

        if (comment == null)
        {
            return Result<CommentCreateResponse>.Failure(new("Комментария не существует"));
        }

        var currentUser = await _currentUserService.GetCurrentUserAsync();

        if (comment.UserId != currentUser.UserId.Value)
        {
            return Result<CommentCreateResponse>.Failure(new("Вы не можете редактировать чужой комментарий"));
        }

        var userEntity = await _repository.Get<UserProfile>()
            .FirstOrDefaultAsync(x => x.UserId == currentUser.UserId.Value);

        _repository.Attach(comment);
        comment.UpdateComment(createRequest.Text);
        await _repository.SaveChangesAsync();

        return new CommentCreateResponse
        {
            Id = comment.Id,
            UserId = currentUser.UserId.Value,
            PhotoUrl = userEntity?.PhotoUrl,
            ReplyTo = comment.ParentId,
            Text = createRequest.Text,
            Username = currentUser.UserName,
            CreatedAt = comment.CreatedAt
        };
    }

    private static CommentListItem MapToCommentListItem(TreeItem<Comment> commentTreeItem, Func<Guid, string?> getUserName)
    {
        return new CommentListItem
        {
            Id = commentTreeItem.Item.Id,
            Text = commentTreeItem.Item.IsDelete ? "Комментарий был удален" : commentTreeItem.Item.Text,
            UserId = commentTreeItem.Item.UserId,
            Username = getUserName(commentTreeItem.Item.UserId),
            CreatedAt = commentTreeItem.Item.CreatedAt,
            Children = commentTreeItem.Children
                .Select(child => MapToCommentListItem(child, getUserName))
                .OrderBy(x => x.CreatedAt)
                .ToList()
        };
    }
}
