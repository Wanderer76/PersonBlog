using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace Infrastructure.Middleware;

public class HeaderClientHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HeaderClientHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = _httpContextAccessor.HttpContext;
        var authorization = context?.Request.Headers.Authorization.ToString();

        if (context?.User.Identity?.IsAuthenticated == true &&
            AuthenticationHeaderValue.TryParse(authorization, out var authenticationHeader))
        {
            request.Headers.Authorization = authenticationHeader;
        }

        if (context?.Request.Headers.TryGetValue(CorrelationMiddleware.CorrelationId, out var correlationId) == true)
        {
            request.Headers.Add(CorrelationMiddleware.CorrelationId, correlationId.ToString());
        }

        return base.SendAsync(request, cancellationToken);
    }
}
