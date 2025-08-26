using Music.Contract.Models;
using Music.Contract.Models.Search;
using Shared.Models;

namespace Music.Domain.Services
{
    public interface ITrackSearchService
    {
        Task<PagedListViewModel<TrackViewItem>> GetTrackByFilterAsync(SearchFilter filter, int page, int size);
    }
}
