using Recommendation.Domain.Entities;

namespace RecommendationDomainTests;

public sealed class InboxMessageTests
{
    [Fact]
    public void MarkFailed_tracks_retries_and_MarkProcessed_completes_message()
    {
        var receivedAt = DateTimeOffset.UtcNow;
        var message = new InboxMessage(Guid.NewGuid(), "PostCatalogChangedV2", receivedAt);

        message.MarkFailed("temporary error");
        message.MarkProcessed(receivedAt.AddSeconds(1));

        Assert.Equal(1, message.RetryCount);
        Assert.True(message.IsProcessed);
        Assert.Null(message.LastError);
    }
}
