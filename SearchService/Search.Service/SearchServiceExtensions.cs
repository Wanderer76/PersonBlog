using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Search.Domain.Services;
using Search.Service.Implementation;
using System.Text.Json;

namespace Search.Service
{
    public static class SearchServiceExtensions
    {
        public static void AddSearchService(this IServiceCollection services, IConfiguration configuration)
        {
            var client = new ElasticsearchClient(new Uri("http://localhost:9200/"));
            services.AddSingleton(client);
            services.AddScoped<ISearchService,ElasticSearchService>();
        }


        public static void UseSearchService(this IApplicationBuilder builder, IConfiguration configuration)
        {
            using var scope = builder.ApplicationServices.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<ElasticsearchClient>();
            var a = configuration.GetSection("PostSearchIndex:IndexSettingsJson").Get<object>();

            client.Transport.Request<StringResponse>(
               Elastic.Transport.HttpMethod.PUT,
               "/post-search",
               PostData.String(JsonSerializer.Serialize(a)));
        }
    }
}
