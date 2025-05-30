using ViewReacting.Domain.Events;

namespace ViewReacting.Domain.Services;

//TODO данный сервис должен отправлять сообщения о событиях, логики быть не должно
public interface IReactionService
{
    Task SetReactionToPost(ReactionCreateModel reaction);
    Task RemoveReactionToPost(Guid postId);
    Task SetViewToPost(VideoViewEvent videoView);
}

public class ReactionCreateModel
{
    public Guid PostId { get; set; }
    public Guid? UserId { get; set; }
    public string? RemoteIp { get; set; }
    public bool? IsLike { get; set; }
    public double Time {  get; set; }
}
