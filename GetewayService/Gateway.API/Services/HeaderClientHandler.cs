using System.Net.Http.Headers;

namespace Gateway.API.Services
{
    public class HeaderClientHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HeaderClientHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Replace("Bearer ", "").Trim());
            }

            if (_httpContextAccessor.HttpContext!.Request.Headers.TryGetValue(CorrelationMiddleware.CorrelationId, out var correlationId))
            {
                request.Headers.Add(CorrelationMiddleware.CorrelationId, correlationId.ToString());
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
