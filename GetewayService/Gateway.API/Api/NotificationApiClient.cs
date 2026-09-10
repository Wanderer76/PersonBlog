using Microsoft.AspNetCore.Http;
using Notification.Contract.Models;
using System.Globalization;
using System.Net.Http.Json;

namespace Gateway.API.Api;

public sealed class NotificationApiClient(HttpClient httpClient)
{
    public Task<NotificationPageResponse> GetNotificationsAsync(string? cursor, int limit, bool unreadOnly,
        CancellationToken cancellationToken)
    {
        var query = new List<KeyValuePair<string, string?>>
        {
            new("limit", limit.ToString(CultureInfo.InvariantCulture)),
            new("unreadOnly", unreadOnly.ToString(CultureInfo.InvariantCulture))
        };
        if (!string.IsNullOrWhiteSpace(cursor)) query.Add(new("cursor", cursor));
        return GetAsync<NotificationPageResponse>(
            $"notifications{QueryString.Create(query)}", cancellationToken);
    }

    public Task<UnreadCountResponse> GetUnreadCountAsync(CancellationToken cancellationToken) =>
        GetAsync<UnreadCountResponse>("notifications/unread-count", cancellationToken);

    public Task MarkReadAsync(Guid id, CancellationToken cancellationToken) =>
        SendWithoutResponseAsync(HttpMethod.Put, $"notifications/{id:D}/read", null, cancellationToken);

    public Task<MarkAllNotificationsReadResponse> MarkAllReadAsync(
        MarkAllNotificationsReadRequest request, CancellationToken cancellationToken) =>
        SendAsync<MarkAllNotificationsReadResponse>(HttpMethod.Put, "notifications/read-all", request,
            cancellationToken);

    public Task<IReadOnlyList<NotificationPreferenceResponse>> GetPreferencesAsync(
        CancellationToken cancellationToken) =>
        GetAsync<IReadOnlyList<NotificationPreferenceResponse>>("notification-preferences", cancellationToken);

    public Task UpdatePreferencesAsync(UpdateNotificationPreferencesRequest request,
        CancellationToken cancellationToken) =>
        SendWithoutResponseAsync(HttpMethod.Put, "notification-preferences", request, cancellationToken);

    private async Task<T> GetAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        return await ReadAsync<T>(response, cancellationToken);
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string requestUri, object? body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, requestUri)
        {
            Content = body is null ? null : JsonContent.Create(body)
        };
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadAsync<T>(response, cancellationToken);
    }

    private async Task SendWithoutResponseAsync(HttpMethod method, string requestUri, object? body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, requestUri)
        {
            Content = body is null ? null : JsonContent.Create(body)
        };
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) throw await CreateExceptionAsync(response, cancellationToken);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode) throw await CreateExceptionAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new InvalidDataException("Notification returned an empty response.");
    }

    private static async Task<DownstreamApiException> CreateExceptionAsync(HttpResponseMessage response,
        CancellationToken cancellationToken) => new(
        "Notification",
        response.StatusCode,
        await response.Content.ReadAsStringAsync(cancellationToken),
        response.Content.Headers.ContentType?.ToString());
}
