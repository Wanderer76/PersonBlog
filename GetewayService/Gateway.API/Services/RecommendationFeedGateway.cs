using Gateway.API.Api;
using Gateway.API.Models.Recommendation;

namespace Gateway.API.Services;

public sealed class RecommendationFeedGateway(
    RecommendationApiClient recommendationClient,
    BlogFeedApiClient blogClient)
{
    public async Task<RecommendationFeedResponse> GetFeedAsync(
        int limit,
        string? cursor,
        Guid? currentPostId,
        RecommendationPostType postType,
        CancellationToken cancellationToken)
    {
        var ranking = await recommendationClient.GetFeedAsync(
            limit,
            cursor,
            currentPostId,
            postType,
            cancellationToken);

        return await HydrateAsync(ranking, cancellationToken);
    }

    private async Task<RecommendationFeedResponse> HydrateAsync(
        RecommendationRankingResponse ranking,
        CancellationToken cancellationToken)
    {
        var cards = await blogClient.GetPostCardsAsync(
            ranking.Items.Select(item => item.PostId).Distinct().ToArray(),
            cancellationToken);
        var cardsById = cards
            .GroupBy(card => card.Id)
            .ToDictionary(group => group.Key, group => group.First());

        var items = ranking.Items
            .Where(item => cardsById.ContainsKey(item.PostId))
            .Select(item =>
            {
                var card = cardsById[item.PostId];
                return new RecommendationFeedItem(
                    item.PostId,
                    item.Reason,
                    card.Title,
                    card.Description,
                    card.PreviewObjectName,
                    card.Creator);
            })
            .ToArray();

        return new RecommendationFeedResponse(
            ranking.RequestId,
            ranking.AlgorithmVersion,
            items,
            ranking.NextCursor);
    }
}
