using System.Net.Mime;
using System.Text;
using System.Text.Json;
using AuthenticationApplication.Models.Requests;

namespace AuthenticationApplication.Service.ApiClient;

public interface IProfileApiAsyncClient
{
    Task<HttpResponseMessage> CreateProfileAsync(ProfileCreateRequest createRequest);
}

public class DefaultProfileApiAsyncClient : IProfileApiAsyncClient
{
    public Task<HttpResponseMessage> CreateProfileAsync(ProfileCreateRequest createRequest)
    {
        var client = new HttpClient();
        var uri = new Uri("http://localhost:5069/create");
        var message = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = uri,
            Content = new StringContent(JsonSerializer.Serialize(createRequest), Encoding.UTF8,
                MediaTypeNames.Application.Json)
        };
        return client.SendAsync(message);
    }
}