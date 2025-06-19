using Elastic.Clients.Elasticsearch;
using Search.Domain;
using Search.Domain.Services;
using Shared.Utils;
using System.Collections.Generic;

namespace Search.Service.Implementation
{
    internal class ElasticSearchService : ISearchService
    {
        private readonly ElasticsearchClient _client;
        private const string Index = "post-search";

        public ElasticSearchService(ElasticsearchClient client)
        {
            _client = client;
        }

        public async Task<Result<IEnumerable<PostModel>>> SearchAsync(SearchOptions query)
        {
            var response = await _client.SearchAsync<PostModel>(s => s
            .Indices(Index)
            .From(query.Skip)
            .Size(query.Take)
               .Query(q => q.MatchPhrasePrefix(m => m
               .Field(f => f.Title)
               .Query(query.Title)
               .MaxExpansions(100)))
               .Sort(so => so
               .Field(f => f.ViewCount, x => x.Order(SortOrder.Desc).NumericType(FieldSortNumericType.Long))
               .Field(f => f.CreatedAt, x => x.Order(SortOrder.Asc).NumericType(FieldSortNumericType.Date))
               )
            );

            if (response.IsValidResponse)
            {
                return Result<IEnumerable<PostModel>>.Success(response.Documents);
            }
            return Result<IEnumerable<PostModel>>.Success([]);
        }

        public async Task<Result<bool>> AddPostAsync(PostModel postModel)
        {
            var response = await _client.IndexAsync(postModel, x => x.Index(Index));
            if (response.IsValidResponse)
            {
                return true;
            }
            return false;
        }

        public async Task<Result<bool>> UpdatePostAsync(PostModel postModel)
        {
            var response = await _client.UpdateAsync<PostModel, PostModel>(postModel.Id, x => x
            .Index(Index)
            .Doc(postModel));

            if (response.IsValidResponse)
            {
                return true;
            }
            return false;
        }

        public async Task<Result<bool>> RemovePostAsync(Guid id)
        {
            var response = await _client.DeleteAsync<PostModel>(id, x => x.Index(Index));

            if (response.IsValidResponse)
            {
                return true;
            }
            return false;
        }
    }
}
