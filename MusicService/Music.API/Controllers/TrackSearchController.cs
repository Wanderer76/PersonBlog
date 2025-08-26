using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Music.Contract.Models;
using Music.Contract.Models.Search;
using Music.Domain.Services;
using Shared.Models;

namespace Music.API.Controllers
{
    public class TrackSearchController : BaseController
    {
        private readonly ITrackSearchService _trackSearchService;
        public TrackSearchController(ILogger<BaseController> logger, ITrackSearchService trackSearchService) : base(logger)
        {
            _trackSearchService = trackSearchService;
        }

        /// <summary>
        /// список треков
        /// </summary>
        /// <param name="page"></param>
        /// <param name="size"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpGet("filtered")]
        [Produces<PagedListViewModel<TrackViewItem>>]
        public async Task<IActionResult> GetTrackFilteredList([FromQuery] int? page, [FromQuery] int? size, [FromQuery] SearchFilter? filter)
        {
            return Ok(await _trackSearchService.GetTrackByFilterAsync(filter ?? new(), page ?? 1, size ?? 10));
        }
    }
}
