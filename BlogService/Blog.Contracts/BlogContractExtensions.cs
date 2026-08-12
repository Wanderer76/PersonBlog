using Infrastructure.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Blog.Contracts;

public static class BlogContractExtensions
{
    public static void AddBlogContract(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<HeaderClientHandler>();

        services.AddHttpClient<BlogApiClient>(x =>
        {
            x.BaseAddress = new Uri(configuration["AppUrls:Blog"]!);
        }).AddHttpMessageHandler<HeaderClientHandler>();

        services.AddHttpClient<PostApiClient>(x =>
        {
            x.BaseAddress = new Uri(configuration["AppUrls:Blog"]!);
        }).AddHttpMessageHandler<HeaderClientHandler>();

#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is required to opt out of the global handler.
        services.AddHttpClient<VideoUploadApiClient>(x =>
        {
            x.BaseAddress = new Uri(configuration["AppUrls:Blog"]!);
            x.Timeout = TimeSpan.FromMinutes(5);
        })
        // Multipart completion can legitimately take longer than the default
        // resilience attempt timeout. Retrying this POST is unsafe and used to
        // make the UI abort an upload that had already completed successfully.
        .RemoveAllResilienceHandlers()
        .AddHttpMessageHandler<HeaderClientHandler>();
#pragma warning restore EXTEXP0001
    }
}
