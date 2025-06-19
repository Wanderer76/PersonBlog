using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Search.Domain.Services;
using Search.Service.Implementation;

namespace Search.Service
{
    public static class SearchServiceExtensions
    {
        public static void AddSearchService(this IServiceCollection services)
        {
            var client = new ElasticsearchClient(new Uri("http://localhost:9200/"));
            services.AddSingleton(client);
            services.AddScoped<ISearchService,ElasticSearchService>();
        }
    }
}
