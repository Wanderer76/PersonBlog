using Microsoft.EntityFrameworkCore;
using Search.Domain.Entities;
using Search.Domain.Models;
using Search.Domain.Services;
using Search.Persistence.Repositories;
using Shared.Utils;
using System.Net.Http.Json;
using System.Text.Json;

namespace Search.Service.Implementation
{
    internal class NpgsqlSearchService : ISearchService
    {
        private readonly NpgSqlSearchRepository _searchRepository;
        private readonly IHttpClientFactory _httpClientFactory;

        public NpgsqlSearchService(NpgSqlSearchRepository searchRepository, IHttpClientFactory httpClientFactory)
        {
            _searchRepository = searchRepository;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<Result<bool>> AddPostAsync(PostModel postModel)
        {
            var isExists = _searchRepository.Get<PostIndex>().AnyAsync(x => x.Id == postModel.Id);
            if (!await isExists)
            {
                using var client = _httpClientFactory.CreateClient("Tokenizer");
                var tokenResponse = await client.PostAsJsonAsync<TokenizerRequest>("tokenize", new TokenizerRequest
                {
                    Text = postModel.Description
                });

                var keywords = tokenResponse.IsSuccessStatusCode
                    ? await JsonSerializer.DeserializeAsync<TokenizerResponse>(await tokenResponse.Content.ReadAsStreamAsync(), new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true })
                    : null;

                var newIndex = new PostIndex
                {
                    Id = postModel.Id,
                    BlogId = postModel.BlogId,
                    CreatedAt = postModel.CreatedAt,
                    Description = postModel.Description ?? "",
                    Title = postModel.Title,
                    ViewCount = postModel.ViewCount,
                    Keywords = keywords?.Tokens == null ? [] : keywords.Tokens.Select(x => new WordScore(x.Word, x.Score)).ToList()
                };
                _searchRepository.Add(newIndex);
                await _searchRepository.SaveChangesAsync();
            }
            return true;
        }

        public async Task<Result<bool>> RemovePostAsync(Guid id)
        {
            await _searchRepository.Get<PostIndex>()
                .Where(x => x.Id == id)
                .ExecuteDeleteAsync();
            return true;
        }

        public async Task<Result<IEnumerable<PostModel>>> SearchAsync(SearchOptions query)
        {
            query.Title = query.Title.Trim();

            var words = query.Title?
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(w => w.Length > 0)
                .ToList() ?? [];
            if (words.Count != 0)
            {
                var result = await _searchRepository.SearchPosts(words, query.Skip, query.Take);
                return result.Select(x => x.ToPostModel()).ToList();
            }
            return Array.Empty<PostModel>();
        }

        public async Task<Result<bool>> UpdatePostAsync(PostModel postModel)
        {
            var index = await _searchRepository.Get<PostIndex>()
                .Include(x => x.Keywords)
                .FirstOrDefaultAsync(x => x.Id == postModel.Id);

            if (index != null)
            {
                using var client = _httpClientFactory.CreateClient("Tokenizer");
                var tokenResponse = await client.PostAsJsonAsync<TokenizerRequest>("tokenize", new TokenizerRequest
                {
                    Text = postModel.Description
                });

                var keywords = tokenResponse.IsSuccessStatusCode
                    ? await JsonSerializer.DeserializeAsync<TokenizerResponse>(await tokenResponse.Content.ReadAsStreamAsync(), new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true })
                    : null;

                _searchRepository.Attach(index);
                {
                    index.Description = postModel.Description ??"";
                    index.Title = postModel.Title;
                    index.ViewCount = postModel.ViewCount;
                    index.Keywords = keywords?.Tokens == null ? [] : keywords.Tokens.Select(x => new WordScore(x.Word, x.Score)).ToList();
                };
                await _searchRepository.SaveChangesAsync();
            }
            return true;
        }
    }
}
