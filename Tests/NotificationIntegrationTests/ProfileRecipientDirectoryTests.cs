using System.Net;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Notification.Infrastructure.Clients;

namespace NotificationIntegrationTests;

public sealed class BlogRecipientDirectoryTests
{
    [Fact]
    public async Task SendsEncodedOpaqueCursorAndUtcCutoff()
    {
        var userId = Guid.NewGuid();
        var handler = new Handler((request, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($"{{\"userIds\":[\"{userId}\"],\"nextCursor\":\"next\",\"hasMore\":true}}", Encoding.UTF8, "application/json")
        }));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://profile.example/") };
        var directory = new BlogRecipientDirectory(client);
        var cutoff = new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.FromHours(5));
        var result = await directory.GetRecipientsPageAsync(Guid.NewGuid(), "a+/=& b", 5, cutoff);
        Assert.True(result.IsSuccess);
        Assert.Equal(userId, Assert.Single(result.Value.UserIds));
        var query = QueryHelpers.ParseQuery(handler.Uri!.Query);
        Assert.Equal("a+/=& b", query["cursor"].ToString());
        Assert.Equal("5", query["limit"].ToString());
        Assert.Equal(cutoff.ToUniversalTime().ToString("O"), query["cutoff"].ToString());
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{")]
    [InlineData("{\"userIds\":null,\"hasMore\":false}")]
    [InlineData("{\"userIds\":[],\"hasMore\":true,\"nextCursor\":\"same\"}")]
    [InlineData("{\"userIds\":[\"00000000-0000-0000-0000-000000000000\"],\"hasMore\":false}")]
    public async Task InvalidPagesReturnFailure(string json)
    {
        using var client = new HttpClient(new Handler((request, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent(json, Encoding.UTF8, "application/json") }))) { BaseAddress = new Uri("https://profile.example/") };
        var result = await new BlogRecipientDirectory(client).GetRecipientsPageAsync(Guid.NewGuid(), "same", 10, DateTimeOffset.UtcNow);
        Assert.Equal("Recipients.InvalidResponse", Assert.Single(result.Errors).Key);
    }

    [Fact]
    public async Task HttpErrorsDoNotExposeResponseBody()
    {
        using var client = new HttpClient(new Handler((request, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        { Content = new StringContent("provider secret") }))) { BaseAddress = new Uri("https://profile.example/") };
        var result = await new BlogRecipientDirectory(client).GetRecipientsPageAsync(Guid.NewGuid(), null, 10, DateTimeOffset.UtcNow);
        Assert.Equal("Recipients.Http503", Assert.Single(result.Errors).Key);
        Assert.DoesNotContain("secret", result.Errors[0].Message);
    }

    [Fact]
    public async Task TransportTimeoutIsFailureButCallerCancellationPropagates()
    {
        using var client = new HttpClient(new Handler((request, ct) => throw new TaskCanceledException()))
        { BaseAddress = new Uri("https://profile.example/") };
        var directory = new BlogRecipientDirectory(client);
        var result = await directory.GetRecipientsPageAsync(Guid.NewGuid(), null, 10, DateTimeOffset.UtcNow);
        Assert.Equal("Recipients.Timeout", Assert.Single(result.Errors).Key);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            directory.GetRecipientsPageAsync(Guid.NewGuid(), null, 10, DateTimeOffset.UtcNow, cancellation.Token));
    }

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        public Uri? Uri { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri = request.RequestUri;
            return send(request, cancellationToken);
        }
    }
}
