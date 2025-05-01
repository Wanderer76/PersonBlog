using System.Net.Mime;
using System.Text;
using System.Text.Json;
using AuthenticationApplication.Models.Requests;
using Microsoft.Extensions.Configuration;
using Shared.Utils;

namespace AuthenticationApplication.Service.ApiClient;

[Obsolete("Не нужен")]
public interface IProfileApiAsyncClient
{
    Task<HttpResponseMessage> CreateProfileAsync(ProfileCreateRequest createRequest);
    Task<HttpResponseMessage> RemoveProfileAsync(Guid userId);
}

public class DefaultProfileApiAsyncClient : IProfileApiAsyncClient
{
    private readonly IConfiguration configuration;

    public DefaultProfileApiAsyncClient(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public async Task<HttpResponseMessage> CreateProfileAsync(ProfileCreateRequest createRequest)
    {
        var path = configuration.GetValue<string>("ProfileUrl:Create");
        path!.AssertFound();

        using var client = new HttpClient();
        var uri = new Uri(path!);
        var body = JsonSerializer.Serialize(createRequest, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        using var message = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = uri,
            Content = new StringContent(body, Encoding.UTF8,
                MediaTypeNames.Application.Json),
        };
        return await client.SendAsync(message);
    }

    public async Task<HttpResponseMessage> RemoveProfileAsync(Guid userId)
    {
        var path = configuration.GetValue<string>("ProfileUrl:Delete");
        path!.AssertFound();
        var prefix = $"/{userId}";
        using var client = new HttpClient();
        var uri = new Uri(path! + prefix);
        using var message = new HttpRequestMessage
        {
            Method = HttpMethod.Delete,
            RequestUri = uri,
        };

        return await client.SendAsync(message);
    }
}