using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Blog.Contracts;

public static class BlogContractExtensions
{
    public static void AddBlogContract(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<BlogApiClient>(x =>
        {
            x.BaseAddress = new Uri(configuration["AppUrls:Blog"]!);
        });

        services.AddHttpClient<PostApiClient>(x =>
        {
            x.BaseAddress = new Uri(configuration["AppUrls:Blog"]!);
        });
    }
}
