using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Search.Domain.Entities;
using Search.Domain.Models;
using Search.Domain.Services;
using Shared.Utils;
using System.Net.Http.Json;
using System.Text.Json;

namespace Search.Service.Implementation
{
    internal class ElasticSearchService : ISearchService
    {
        private readonly ElasticsearchClient _client;
        private const string Index = "post-search";
        private readonly IHttpClientFactory _httpClientFactory;

        public ElasticSearchService(ElasticsearchClient client, IHttpClientFactory httpClientFactory)
        {
            _client = client;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<Result<IEnumerable<PostModel>>> SearchAsync(SearchOptions query)
        {
            query.Title = query.Title.Trim();
           
            var words = query.Title?
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(w => w.Length > 0)
                .ToList() ?? [];

            var shouldQueries = new List<Query>();

            // Поиск по title
            foreach (var word in words)
            {
                shouldQueries.Add(new MatchQuery("title")
                {
                    Query = word,
                    Boost = 5,
                    Fuzziness = new Fuzziness("AUTO")
                });
            }

            // Поиск по keywords.word
            foreach (var word in words)
            {
                shouldQueries.Add(new NestedQuery
                {
                    Path = "keywords",
                    Query = new ScriptScoreQuery
                    {
                        Query = new MatchQuery("keywords.word")
                        {
                            Query = word,
                            Fuzziness = new Fuzziness("AUTO")
                        },
                        Script = new Script
                        {
                            Source = """
                    if (doc['keywords.score'].size() == 0 || doc['keywords.score'].value == 0) {
                        return _score;
                    } else {
                        return _score * doc['keywords.score'].value;
                    }
                """
                        }
                    }
                });
            }

            var searchRequest = new SearchRequest<PostIndex>("post-search")
            {
                Size = 10,
                Query = new BoolQuery
                {
                    Should = shouldQueries,
                    MinimumShouldMatch = 1
                },
                Sort = new List<SortOptions>
                {
                    SortOptions.Field(new Field("viewCount"), new FieldSort
                    {
                        Order = SortOrder.Desc,
                        NumericType = FieldSortNumericType.Long
                    }),
                    SortOptions.Field("createdAt", new FieldSort
                    {
                        Order = SortOrder.Desc,
                        NumericType = FieldSortNumericType.Date
                    })
                }
            };

            var response = await _client.SearchAsync<PostIndex>(searchRequest);

            if (response.IsValidResponse)
            {
                return Result<IEnumerable<PostModel>>.Success(response.Documents.Select(x => x.ToPostModel()));
            }
            return Result<IEnumerable<PostModel>>.Success([]);
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
