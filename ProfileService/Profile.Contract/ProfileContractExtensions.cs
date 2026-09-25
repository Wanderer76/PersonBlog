using Infrastructure.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Profile.Contract.HttpClients;
using System;
using System.Collections.Generic;
using System.Text;

namespace Profile.Contract;

public static class ProfileContractExtensions
{
    public static IHttpClientBuilder AddProfileHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("Profile", x =>
        {
            x.BaseAddress = new Uri(configuration["AppUrls:Profile"]);
        }).AddHttpMessageHandler<HeaderClientHandler>();

        return services.AddHttpClient<ProfileHttpClient>(x =>
        {
            x.BaseAddress = new Uri(configuration["AppUrls:Profile"]);
        }).AddHttpMessageHandler<HeaderClientHandler>();
    }
}
