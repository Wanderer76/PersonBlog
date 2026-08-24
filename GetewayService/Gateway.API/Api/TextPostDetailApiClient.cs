using Gateway.API.Models.TextPost;
using System.Net.Http.Json;

namespace Gateway.API.Api;

public sealed class TextPostDetailApiClient(HttpClient httpClient)
{
    public async Task<TextPostDetailResponse> GetDetailAsync(
        Guid postId,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"TextPost/detail/{postId}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw await CreateExceptionAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<TextPostDetailResponse>(cancellationToken)
            ?? throw new InvalidDataException("Blog returned an empty text post response.");
    }

    public async Task RegisterViewAsync(
        Guid postId,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync(
            $"Post/setView/{postId}",
            content: null,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw await CreateExceptionAsync(response, cancellationToken);
    }

    private static async Task<DownstreamApiException> CreateExceptionAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken) => new(
        "Blog",
        response.StatusCode,
        await response.Content.ReadAsStringAsync(cancellationToken),
        response.Content.Headers.ContentType?.ToString());
}
