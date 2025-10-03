using Music.Contract.Models;
using Shared.Utils;

namespace Music.Domain.Services
{
    public interface IGenreService
    {
        Task<Result<IReadOnlyList<GenreItem>>> GetGenreListAsync();
    }
}
