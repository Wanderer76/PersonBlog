using System.ComponentModel.DataAnnotations;

namespace MusicRecommendation.Domain.Domain;

public class UserListenHistory : IRecommendation
{
    [Key]
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid TrackId { get; private set; }

    private UserListenHistory()
    {
        
    }

    public UserListenHistory(Guid id, Guid userId, DateTimeOffset createdAt, Guid trackId)
    {
        Id = id;
        UserId = userId;
        CreatedAt = createdAt;
        TrackId = trackId;
    }
}
