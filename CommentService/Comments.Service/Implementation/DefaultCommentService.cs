using Comments.Domain.Entities;
using Comments.Contracts.Events;
using Comments.Domain.Models;
using Comments.Domain.Services;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Profile.Service.HttpClients;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;

namespace Comments.Service.Implementation;

internal class DefaultCommentService : ICommentService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IReadWriteRepository<ICommentEntity> _repository;
    private readonly ProfileHttpClient _profileHttpClient;
    public DefaultCommentService(ICurrentUserService currentUserService, IReadWriteRepository<ICommentEntity> repository, ProfileHttpClient profileHttpClient)
    {
        _currentUserService = currentUserService;
        _repository = repository;
        this._profileHttpClient = profileHttpClient;
    }

    public async Task<Result<CommentCreateResponse>> CreateCommentAsync(CommentCreateRequest createRequest)
    {
        Comment? parent = null;
        if (createRequest.ReplyTo.HasValue)
        {
            parent = await _repository.Get<Comment>()
                .FirstOrDefaultAsync(x => x.Id == createRequest.ReplyTo.Value);
            if (parent is null)
            {
                return Result<CommentCreateResponse>.Failure(new Error("Комментария не существует"));
            }
            if (parent.PostId != createRequest.PostId)
                return Result<CommentCreateResponse>.Failure(new Error(nameof(createRequest.PostId),
                    "Родительский комментарий относится к другой публикации"));
        }

        var user = await _currentUserService.GetCurrentUserAsync();
        var comment = new Comment(user.UserId, createRequest.PostId, createRequest.Text, createRequest.ReplyTo);
        _repository.Add(comment);
        if (parent is not null)
        {
            var eventId = GuidService.GetNewGuid();
            _repository.Add(CommentOutboxMessage.Create(new CommentReplyCreatedV1
            {
                EventId = eventId,
                OccurredAt = comment.CreatedAt.ToUniversalTime(),
                CommentId = comment.Id,
                ParentCommentId = parent.Id,
                PostId = comment.PostId,
                ActorUserId = comment.UserId,
                RecipientUserId = parent.UserId
            }, eventId));
        }
        await _repository.SaveChangesAsync();
        var userEntity = await _repository.Get<UserProfile>()
            .FirstOrDefaultAsync(x => x.UserId == user.UserId);

        return new CommentCreateResponse
        {
            Id = comment.Id,
            UserId = user.UserId,
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


        var userIds = (await Task.WhenAll(comments.Select(x => x.UserId)
            .Distinct()
            .Select(x => _profileHttpClient.GetProfileByUserIdAsync(x))))
            .ToDictionary(x => x.UserId);

        //var userIds = (await _repository.Get<UserProfile>()
        //    .Join(_repository.Get<Comment>().Where(x => x.PostId == postId),
        //    outer => outer.UserId,
        //    inner => inner.UserId,
        //    (x, y) => x)
        //    .Distinct()
        //    .ToDictionaryAsync(x => x.UserId))!;



        var result = comments.ToTree(c => c.Id, c => c.ParentId)
            .Select(x => MapToCommentListItem(x, (userId) => userIds.TryGetValue(userId, out var user) ? user.Name : null))
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        return new CommentsListViewModel(commentsCount, result);
    }


    public async Task<Result> RemoveCommentAsync(Guid commentId)
    {
        var comment = await _repository.Get<Comment>()
            .FirstOrDefaultAsync(x => x.Id == commentId);

        if (comment == null)
        {
            return Result.Failure(nameof(commentId), "Комментария не существует");
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
            return Result<CommentCreateResponse>.Failure(new Error("Комментария не существует"));
        }

        var currentUser = await _currentUserService.GetCurrentUserAsync();

        if (comment.UserId != currentUser.UserId)
        {
            return Result<CommentCreateResponse>.Failure(new Error("Вы не можете редактировать чужой комментарий"));
        }

        var userEntity = await _repository.Get<UserProfile>()
            .FirstOrDefaultAsync(x => x.UserId == currentUser.UserId);

        _repository.Attach(comment);
        comment.UpdateComment(createRequest.Text);
        await _repository.SaveChangesAsync();

        return new CommentCreateResponse
        {
            Id = comment.Id,
            UserId = currentUser.UserId,
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
