using Gateway.API.Api;
using Infrastructure.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Controllers.Internal
{
    public class PostController : BaseApiController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public PostController(ILogger<BaseApiController> logger, IHttpClientFactory httpClientFactory) : base(logger)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("post/{id}")]
        public async Task<IActionResult> GetPostInfoById(Guid id)
        {
            var result = await _httpClientFactory.GetPostDetailViewAsync(HttpContext, id);
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            return BadRequest(result.Errors);
        }

    }
}
