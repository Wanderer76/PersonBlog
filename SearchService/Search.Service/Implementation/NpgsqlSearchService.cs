using Search.Domain.Models;
using Search.Domain.Services;
using Search.Persistence.Repositories;
using Shared.Utils;

namespace Search.Service.Implementation
{
    internal class NpgsqlSearchService : ISearchService
    {
        private readonly NpgSqlSearchRepository _searchRepository;

        public NpgsqlSearchService(NpgSqlSearchRepository searchRepository)
        {
            _searchRepository = searchRepository;
        }

        public Task<Result<bool>> AddPostAsync(PostModel postModel)
        {
            throw new NotImplementedException();
        }

        public Task<Result<bool>> RemovePostAsync(Guid id)
        {
            throw new NotImplementedException();
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
                var result = await _searchRepository.SearchPosts(words,query.Skip,query.Take);
                return result.Select(x=>x.ToPostModel()).ToList();
            }
            return Array.Empty<PostModel>();
        }

        public Task<Result<bool>> UpdatePostAsync(PostModel postModel)
        {
            throw new NotImplementedException();
        }
    }
}
