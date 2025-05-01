using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Infrastructure.Extensions
{
    public static class CacheExtensions
    {
        public static void AddRedisCache(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<ICacheService, RedisCacheService>();
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration["Redis:ConnectionString"];
                options.InstanceName = configuration["Redis:InstanceName"];
            });
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                var connectionString = configuration["Redis:ConnectionString"]!;
                return ConnectionMultiplexer.Connect(connectionString);
            });

        }
    }
}
