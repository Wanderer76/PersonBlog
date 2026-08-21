namespace Blog.Contracts.Models;

public class PostCommonModel
{
    public Guid Id { get; set; }
    public string? PreviewObjectName { get; set; }
    public string? Description { get; set; }
    public string Title { get; set; }
    public PostCreatorModel Creator { get; set; } = null!;
}

public sealed class PostCreatorModel
{
    public Guid UserId { get; set; }
    public Guid BlogId { get; set; }
    public string Name { get; set; } = null!;
    public string? AvatarUrl { get; set; }
}

public class PostCommonModelV2
{
    public Guid Id { get; set; }
    public Guid BlogId {  get; set; }
    public string Title { get; set; } = null!;
    public PostTypeModel PostType { get; set; }
    public string? PreviewUrl { get; set; }
    public string? Text { get; set; }
}

public sealed class UserPostCommonModel : PostCommonModelV2
{
    public bool CanView { get; set; }
}

public enum PostTypeModel
{
    Text,
    Video,
}
