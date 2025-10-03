using Music.Contract.Models.Artist;
using Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Music.Domain.Services
{
    public interface IArtistSearchService
    {
        Task<Result<IReadOnlyList<ArtistInfo>>> SearchArtistByName(string artistName);
    }
}
