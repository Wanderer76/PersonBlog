using Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Security.Claims;

namespace GatewayApiTests;

public sealed class HeaderClientHandlerTests
{
    [Fact]
    public async Task SendAsync_ForwardsRecommendationIdentityHeaders()
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            "test"));
        context.Request.Headers.Authorization = "Bearer access-token";
        context.Request.Headers[CorrelationMiddleware.CorrelationId] = "correlation-id";

        HttpRequestMessage? forwardedRequest = null;
        var handler = new HeaderClientHandler(new HttpContextAccessor { HttpContext = context })
        {
            InnerHandler = new CaptureHandler(request => forwardedRequest = request)
        };
        using var invoker = new HttpMessageInvoker(handler);

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://service/feed"), CancellationToken.None);

        Assert.NotNull(forwardedRequest);
        Assert.Equal("Bearer", forwardedRequest.Headers.Authorization?.Scheme);
        Assert.Equal("access-token", forwardedRequest.Headers.Authorization?.Parameter);
        Assert.Equal("correlation-id", forwardedRequest.Headers.GetValues(CorrelationMiddleware.CorrelationId).Single());
    }

    private sealed class CaptureHandler(Action<HttpRequestMessage> capture) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            capture(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
