using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Extensions;

public static class HttpClientExtensions
{
    public static HttpClient CreateClientContextHeaders(this IHttpClientFactory factory, string type, HttpContext httpContext)
    {
        var client = factory.CreateClient(type);
        foreach (var i in httpContext.Request.Headers)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(i.Key, i.Value.ToArray());

        }
        return client;
    }
}
