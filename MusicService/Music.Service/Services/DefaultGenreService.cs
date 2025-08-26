using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Music.Contract.Models;
using Music.Domain.Entities;
using Music.Domain.Services;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;

namespace Music.Service.Services
{
    internal class DefaultGenreService : IGenreService
    {
        private readonly IReadRepository<IMusicEntity> _repository;
        private readonly ICacheService _cacheService;

        public DefaultGenreService(IReadRepository<IMusicEntity> repository, ICacheService cacheService)
        {
            _repository = repository;
            _cacheService = cacheService;
        }

        public async Task<Result<IReadOnlyList<GenreItem>>> GetGenreListAsync()
        {
            var result = await _cacheService.GetOrAddDataAsync(new GenreListCacheKey(), async () =>
            {
                var data = await _repository.Get<Genre>()
                .OrderBy(x => x.Name)
                .Select(x => new GenreItem(x.Id, x.Name))
                .ToListAsync();
                return data;
            });
            return result;
        }
    }

    file class GenreListCacheKey : ICacheKey
    {
        private const string Key = nameof(GenreListCacheKey);
        public string GetKey() => Key;
    }
}
