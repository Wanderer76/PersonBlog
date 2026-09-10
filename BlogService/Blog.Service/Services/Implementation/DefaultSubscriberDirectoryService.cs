using System.Globalization;
using System.Text;
using Blog.Contracts.Models.Blog;
using Blog.Contracts.Services;
using Blog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Utils;

namespace Blog.Service.Services.Implementation;

internal sealed class DefaultSubscriberDirectoryService(IReadWriteRepository<IBlogEntity> repository)
    : ISubscriberDirectoryService
{
    public async Task<Result<SubscriberPage>> GetPageAsync(Guid blogId, string? cursor, int limit,
        DateTimeOffset cutoff, CancellationToken cancellationToken = default)
    {
        if (blogId == Guid.Empty)
            return Result<SubscriberPage>.Failure(new Error(nameof(blogId), "Blog identifier is required."));
        if (limit is < 1 or > 500)
            return Result<SubscriberPage>.Failure(new Error(nameof(limit), "Limit must be between 1 and 500."));
        if (!TryDecodeCursor(cursor, out var boundary))
            return Result<SubscriberPage>.Failure(new Error(nameof(cursor), "Cursor is invalid."));

        var snapshot = cutoff.ToUniversalTime();
        var query = repository.Get<Subscriber>().AsNoTracking()
            .Where(item => item.BlogId == blogId &&
                item.SubscriptionStartDate <= snapshot &&
                (item.SubscriptionEndDate == null || item.SubscriptionEndDate > snapshot));

        if (boundary is not null)
        {
            query = query.Where(item => item.SubscriptionStartDate > boundary.Value.CreatedAt ||
                (item.SubscriptionStartDate == boundary.Value.CreatedAt &&
                 item.UserId.CompareTo(boundary.Value.UserId) > 0));
        }

        var rows = await query.OrderBy(item => item.SubscriptionStartDate)
            .ThenBy(item => item.UserId)
            .Select(item => new CursorItem(item.SubscriptionStartDate, item.UserId))
            .Take(limit + 1)
            .ToArrayAsync(cancellationToken);
        var page = rows.Take(limit).ToArray();
        var hasMore = rows.Length > limit;
        var nextCursor = hasMore && page.Length > 0 ? EncodeCursor(page[^1]) : null;
        return new SubscriberPage(page.Select(item => item.UserId).ToArray(), nextCursor, hasMore);
    }

    private static string EncodeCursor(CursorItem item)
    {
        var value = string.Create(CultureInfo.InvariantCulture,
            $"{item.CreatedAt.ToUniversalTime().Ticks}:{item.UserId:D}");
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    private static bool TryDecodeCursor(string? cursor, out CursorItem? item)
    {
        item = null;
        if (string.IsNullOrEmpty(cursor)) return true;
        try
        {
            var value = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var separator = value.IndexOf(':');
            if (separator < 1 ||
                !long.TryParse(value[..separator], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks) ||
                !Guid.TryParse(value[(separator + 1)..], out var userId) ||
                userId == Guid.Empty)
                return false;
            item = new CursorItem(new DateTimeOffset(ticks, TimeSpan.Zero), userId);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private readonly record struct CursorItem(DateTimeOffset CreatedAt, Guid UserId);
}
