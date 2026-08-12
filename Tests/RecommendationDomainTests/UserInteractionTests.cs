using Recommendation.Domain.Entities;
using Recommendation.Domain.Enums;

namespace RecommendationDomainTests;

public sealed class UserInteractionTests
{
    [Fact]
    public void Constructor_requires_exactly_one_subject()
    {
        Assert.Throws<ArgumentException>(() => Create(userId: null, anonymousSessionId: null));
        Assert.Throws<ArgumentException>(() => Create(Guid.NewGuid(), "session"));
    }

    [Fact]
    public void Constructor_validates_reaction_semantics()
    {
        Assert.Throws<ArgumentException>(() => Create(
            Guid.NewGuid(),
            null,
            InteractionType.Like,
            reaction: false));

        var interaction = Create(
            Guid.NewGuid(),
            null,
            InteractionType.Like,
            reaction: true);

        Assert.True(interaction.Reaction);
    }

    private static UserInteraction Create(
        Guid? userId,
        string? anonymousSessionId,
        InteractionType type = InteractionType.Open,
        bool? reaction = null) => new(
            Guid.NewGuid(),
            userId,
            anonymousSessionId,
            Guid.NewGuid(),
            type,
            DateTimeOffset.UtcNow,
            reaction: reaction);
}
