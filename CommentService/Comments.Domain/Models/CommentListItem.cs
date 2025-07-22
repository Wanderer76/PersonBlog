using Comments.Domain.Entities;

namespace Comments.Domain.Models;


public class CommentsListViewModel
{
    public int Count { get; private set; }

    public IReadOnlyList<CommentListItem> Comments { get; private set; }

    public CommentsListViewModel(int count, IReadOnlyList<CommentListItem> comments)
    {
        Count = count;
        Comments = comments;
    }
}

public class CommentListItem
{
    public required Guid Id { get; set; }
    public required string Text { get; set; }
    public required Guid UserId { get; set; }
    public required string? Username { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required List<CommentListItem> Children { get; set; }
}
