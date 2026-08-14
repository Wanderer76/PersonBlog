using System.Net;

namespace Gateway.API.Api;

internal sealed class DownstreamApiException(
    string serviceName,
    HttpStatusCode statusCode,
    string? responseBody,
    string? contentType)
    : Exception($"{serviceName} returned HTTP {(int)statusCode}.")
{
    public string ServiceName { get; } = serviceName;
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string? ResponseBody { get; } = responseBody;
    public string? ContentType { get; } = contentType;
}
