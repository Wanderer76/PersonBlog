using Profile.Domain.Entities;

namespace Profile.Application.Models.Profile;

public class ProfileModel
{
    public required long Id { get; set; }
    public required string Name { get; set; }
    public required Guid UserId { get; set; }
    public required string? PhotoUrl { get; set; }
    public required ProfileState ProfileState { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public IReadOnlyList<string> Interests { get; set; } = [];
}