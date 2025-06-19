using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Search.Domain;
using Search.Domain.Services;
using Shared.Utils;

namespace SearchService.Application.Controllers
{
    public class PostSearchController : BaseController
    {
        private readonly ISearchService _searchService;

        public PostSearchController(ILogger<BaseController> logger, ISearchService searchService) : base(logger)
        {
            _searchService = searchService;
        }

        [HttpGet]
        [Produces<Result<IEnumerable<PostModel>>>]
        public async Task<IActionResult> Search([FromQuery] SearchOptions model)
        {
            var a = await _searchService.SearchAsync(model);
            return Ok(a.Value);
        }
    }
}
