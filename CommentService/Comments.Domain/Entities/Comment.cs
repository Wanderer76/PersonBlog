using Infrastructure.Interface;
using Shared.Services;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comments.Domain.Entities;

public class Comment : ICommentEntity, ISoftDelete
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid PostId { get; private set; }
    public string Text { get; private set; }
    public Guid? ParentId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public bool IsDelete { get; private set; }
    public DateTimeOffset? DeleteDateTime { get; private set; }

    [ForeignKey(nameof(ParentId))]
    public Comment? Parent { get; private set; }

    public List<Comment> Children { get; private set; }

    public Comment(Guid userId, Guid postId, string text, Guid? parentId)
    {
        Id = GuidService.GetNewGuid();
        UserId = userId;
        PostId = postId;
        Text = text;
        ParentId = parentId;
        IsDelete = false;
        CreatedAt = DateTimeService.Now();
    }

    public void Remove()
    {
        IsDelete = true;
        DeleteDateTime = DateTimeService.Now();
    }

    public void Restore()
    {
        IsDelete = false;
        DeleteDateTime = null;
    }

    public void UpdateComment(string text)
    {
        Text = text;
        CreatedAt = DateTimeService.Now();
    }
}
