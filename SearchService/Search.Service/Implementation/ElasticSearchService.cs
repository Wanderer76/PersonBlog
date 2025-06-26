using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Analysis;
using Search.Domain;
using Search.Domain.Services;
using Shared.Utils;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace Search.Service.Implementation
{
    internal class ElasticSearchService : ISearchService
    {
        private readonly ElasticsearchClient _client;
        private const string Index = "post-search-v2";
        private readonly IHttpClientFactory _httpClientFactory;

        public ElasticSearchService(ElasticsearchClient client, IHttpClientFactory httpClientFactory)
        {
            _client = client;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<Result<IEnumerable<PostModel>>> SearchAsync(SearchOptions query)
        {
            query.Title = query.Title.Trim();
            var response = await _client.SearchAsync<PostIndex>(s => s
            .Index(Index)
            .From(query.Skip)
            .Size(query.Take)
            .Query(q => q
            .Bool(b => b
            .Should(
                // Поиск по началу слов в Title
                s1 => s1.Wildcard(m => m
                .Field(f => f.Title)
                .Value($"*{query.Title}*")
                .CaseInsensitive(true)
                .Boost(5)
                ),
                // Поиск по вложенному keywords.word, включая неполные слова
                s2 => s2.Nested(n => n
                .Path(p => p.Keywords)
                .Query(nq => nq
                .MatchPhrasePrefix(m => m
                            .Field("keywords.word")
                            .Query(query.Title)
                            .MaxExpansions(50)
                            .Boost(1)))))
            ))
            .Sort(srt => srt
            .Field(f => f.ViewCount, x => x.Order(SortOrder.Desc).NumericType(FieldSortNumericType.Long))
            .Field(f => f.CreatedAt, x => x.Order(SortOrder.Asc).NumericType(FieldSortNumericType.Date))));

            if (response.IsValidResponse)
            {
                return Result<IEnumerable<PostModel>>.Success(response.Documents.Select(x => x.ToPostModel()));
            }
            return Result<IEnumerable<PostModel>>.Success([]);
        }

        class TokenizerRequest
        {
            public string Text { get; set; }
        }

        class TokenizerResponse
        {
            public List<WordScore> Tokens { get; set; }
        }

        public async Task<Result<bool>> AddPostAsync(PostModel postModel)
        {
            using var client = _httpClientFactory.CreateClient("Tokenizer");
            var tokenResponse = await client.PostAsJsonAsync<TokenizerRequest>("tokenize", new TokenizerRequest
            {
                Text = postModel.Description
            });

            var keywords = tokenResponse.IsSuccessStatusCode
                ? await JsonSerializer.DeserializeAsync<TokenizerResponse>(await tokenResponse.Content.ReadAsStreamAsync(), new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true })
                : null;

            var index = new PostIndex
            {
                Id = postModel.Id,
                BlogId = postModel.BlogId,
                CreatedAt = postModel.CreatedAt,
                Title = postModel.Title,
                Description = postModel.Description,
                ViewCount = postModel.ViewCount,
                Keywords = keywords?.Tokens ?? []
            };
            var response = await _client.IndexAsync(index, x => x.Index(Index));
            if (response.IsValidResponse)
            {
                return true;
            }
            return false;
        }

        public async Task<Result<bool>> UpdatePostAsync(PostModel postModel)
        {
            using var client = _httpClientFactory.CreateClient("Tokenizer");
            var tokenResponse = await client.PostAsJsonAsync<TokenizerRequest>("tokenizer", new TokenizerRequest
            {
                Text = postModel.Description
            });

            var keywords = tokenResponse.IsSuccessStatusCode
                ? await JsonSerializer.DeserializeAsync<TokenizerResponse>(await tokenResponse.Content.ReadAsStreamAsync())
                : null;
            var updated = new PostIndex
            {
                Id = postModel.Id,
                BlogId = postModel.BlogId,
                CreatedAt = postModel.CreatedAt,
                Title = postModel.Title,
                Description = postModel.Description,
                ViewCount = postModel.ViewCount,
                Keywords = keywords?.Tokens ?? []
            };

            var response = await _client.UpdateAsync<PostIndex, PostIndex>(postModel.Id, x => x
            .Index(Index)
            .Doc(updated));

            if (response.IsValidResponse)
            {
                return true;
            }
            return false;
        }

        public async Task<Result<bool>> RemovePostAsync(Guid id)
        {
            var response = await _client.DeleteAsync<PostIndex>(id, x => x.Index(Index));

            if (response.IsValidResponse)
            {
                return true;
            }
            return false;
        }
    }
}
