using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Notification.Application.Abstractions;
using Shared.Utils;

namespace Notification.Infrastructure.Clients;

/// <summary>Reads the Profile internal subscriber API; authentication is configured on the HTTP client.</summary>
public sealed class ProfileRecipientDirectory(HttpClient client) : IRecipientDirectory
{
    public async Task<Result<RecipientPage>> GetRecipientsPageAsync(Guid blogId, string? cursor, int limit,
        DateTimeOffset cutoff, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (blogId == Guid.Empty || limit is < 1 or > 500)
            return Failure("Recipients.InvalidRequest", "A blog id and a limit between 1 and 500 are required.");
        var uri = QueryHelpers.AddQueryString($"api/internal/blogs/{blogId:D}/subscribers",
            new Dictionary<string, string?>
            {
                ["cursor"] = cursor,
                ["limit"] = limit.ToString(CultureInfo.InvariantCulture),
                ["cutoff"] = cutoff.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
            });
        try
        {
            using var response = await client.GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Failure($"Recipients.Http{(int)response.StatusCode}", "Profile could not return recipients.");
            var page = await response.Content.ReadFromJsonAsync<RecipientPage>(cancellationToken);
            if (page?.UserIds is null || page.UserIds.Count > limit || page.UserIds.Any(x => x == Guid.Empty) ||
                (page.HasMore && (string.IsNullOrWhiteSpace(page.NextCursor) || page.NextCursor == cursor)))
                return Failure("Recipients.InvalidResponse", "Profile returned an invalid recipient page.");
            return page;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Failure("Recipients.Timeout", "Profile request timed out.");
        }
        catch (HttpRequestException)
        {
            return Failure("Recipients.Unavailable", "Profile is unavailable.");
        }
        catch (JsonException)
        {
            return Failure("Recipients.InvalidResponse", "Profile returned invalid JSON.");
        }
    }

    private static Result<RecipientPage> Failure(string key, string message) =>
        Result<RecipientPage>.Failure(new Error(key, message));
}
