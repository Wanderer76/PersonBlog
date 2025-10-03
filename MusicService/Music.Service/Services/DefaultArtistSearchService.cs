using Microsoft.EntityFrameworkCore;
using Music.Contract.Models.Artist;
using Music.Domain.Entities;
using Music.Domain.Services;
using Shared.Persistence;
using Shared.Utils;

namespace Music.Service.Services
{
    internal class DefaultArtistSearchService : IArtistSearchService
    {
        private readonly IReadRepository<IMusicEntity> _repository;

        public DefaultArtistSearchService(IReadRepository<IMusicEntity> repository)
        {
            _repository = repository;
        }

        public async Task<Result<IReadOnlyList<ArtistInfo>>> SearchArtistByName(string artistName)
        {
            return await _repository.Get<Artist>()
                .Where(x => EF.Functions.ILike(x.Name, $"%{artistName}%"))
                .Select(x => new ArtistInfo(x.Id, x.Name))
                .ToListAsync();
        }
    }
}
