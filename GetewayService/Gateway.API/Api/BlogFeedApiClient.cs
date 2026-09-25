using Gateway.API.Models.Recommendation;
using System.Net.Http.Json;

namespace Gateway.API.Api;

public sealed class BlogFeedApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<BlogPostCard>> GetPostCardsAsync(
        IReadOnlyCollection<Guid> postIds,
        CancellationToken cancellationToken)
    {
        if (postIds.Count == 0)
            return [];

        using var response = await httpClient.PostAsJsonAsync(
            "Post/commonByIds",
            postIds,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new DownstreamApiException(
                "Blog",
                response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken),
                response.Content.Headers.ContentType?.ToString());
        }

        return await response.Content.ReadFromJsonAsync<List<BlogPostCard>>(cancellationToken) ?? [];
    }
}
