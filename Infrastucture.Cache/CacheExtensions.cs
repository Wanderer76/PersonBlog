using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Cache
{
    public static class CacheExtensions
    {
        public static void AddRedisCahce(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration["Redis:ConnectionString"];
                options.InstanceName = configuration["Redis:InstanceName"];
            });
        }
    }

    file class RedisConfig
    {
        string ConnectionString;
        string InstanceName;
    }

}
