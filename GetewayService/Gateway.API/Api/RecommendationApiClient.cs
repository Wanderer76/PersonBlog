using Gateway.API.Models.Recommendation;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Json;

namespace Gateway.API.Api;

public sealed class RecommendationApiClient(HttpClient httpClient)
{
    public Task<RecommendationRankingResponse> GetFeedAsync(
        int limit,
        string? cursor,
        Guid? currentPostId,
        RecommendationPostType postType,
        CancellationToken cancellationToken)
    {
        var query = new List<KeyValuePair<string, string?>>
        {
            new("limit", limit.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("postType", postType.ToString())
        };

        if (!string.IsNullOrWhiteSpace(cursor))
            query.Add(new("cursor", cursor));
        if (currentPostId.HasValue)
            query.Add(new("currentPostId", currentPostId.Value.ToString()));

        return GetAsync($"v1/feed{QueryString.Create(query)}", cancellationToken);
    }

    private async Task<RecommendationRankingResponse> GetAsync(
        string requestUri,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw await CreateExceptionAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<RecommendationRankingResponse>(cancellationToken)
            ?? throw new InvalidDataException("Recommendation returned an empty response.");
    }

    private static async Task<DownstreamApiException> CreateExceptionAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken) => new(
        "Recommendation",
        response.StatusCode,
        await response.Content.ReadAsStringAsync(cancellationToken),
        response.Content.Headers.ContentType?.ToString());
}
