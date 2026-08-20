using Recommendation.Domain.Entities;
using Recommendation.Domain.Enums;

namespace RecommendationDomainTests;

public sealed class UserInteractionTests
{
    [Fact]
    public void Create_requires_exactly_one_subject()
    {
        Assert.True(Create(userId: null, anonymousSessionId: null).IsFailure);
        Assert.True(Create(Guid.NewGuid(), "session").IsFailure);
    }

    [Fact]
    public void Create_validates_reaction_semantics()
    {
        Assert.True(Create(
            Guid.NewGuid(),
            null,
            InteractionType.Like,
            reaction: false).IsFailure);

        var result = Create(
            Guid.NewGuid(),
            null,
            InteractionType.Like,
            reaction: true);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Reaction);
    }

    private static Result<UserInteraction> Create(
        Guid? userId,
        string? anonymousSessionId,
        InteractionType type = InteractionType.Open,
        bool? reaction = null) => UserInteraction.Create(
            Guid.NewGuid(),
            userId,
            anonymousSessionId,
            Guid.NewGuid(),
            type,
            DateTimeOffset.UtcNow,
            reaction: reaction);
}
