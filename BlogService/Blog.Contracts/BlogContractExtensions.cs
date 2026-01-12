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
    }
}
