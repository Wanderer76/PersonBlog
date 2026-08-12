using Blog.Contracts.Models.Upload;
using Blog.Contracts.Services;
using Infrastructure.Services;
using System.Net.Http.Json;

namespace Blog.Contracts;

public sealed class VideoUploadApiClient(HttpClient httpClient)
{
    public async Task<MultipartUploadSession> InitiateUploadAsync(InitiateUploadRequest request)
    {
        var response = await httpClient.PostAsJsonAsync("VideoUpload/initiate", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MultipartUploadSession>())!;
    }

    public async Task<PreSignedUrl> GenerateUrlAsync(GenerateUrlRequest request)
    {
        var response = await httpClient.PostAsJsonAsync("VideoUpload/generate-url", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PreSignedUrl>())!;
    }

    public async Task<string?> CompleteUploadAsync(CompleteUploadRequest request)
    {
        var response = await httpClient.PostAsJsonAsync("VideoUpload/complete", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task AbortUploadAsync(AbortUploadRequest request)
    {
        var response = await httpClient.PostAsJsonAsync("VideoUpload/abort", request);
        response.EnsureSuccessStatusCode();
    }
}
