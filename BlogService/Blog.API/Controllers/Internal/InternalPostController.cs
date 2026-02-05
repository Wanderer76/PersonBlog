using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace Blog.API.Controllers.Internal
{
    public class InternalPostController(ILogger<BaseApiController> logger) : BaseApiController(logger)
    {
        [HttpGet("blogSubscribers/{blogId}")]
        public async Task<IActionResult> GetBlogSubscribers(Guid blogId, int page, int pageSize)
        {
            //Response.Headers.ContentType = "application/x-ndjson"; // или text/plain
            //var query = _repository.Get<Subscriber>()
            //    .Active()
            //    .Where(x => x.BlogId == blogId)
            //    .Select(x => x.UserId)
            //    .Skip((page - 1) * pageSize)
            //    .Take(pageSize);

            //await foreach (var subscriber in query.AsAsyncEnumerable())
            //{
            //    yield return subscriber.ToString();
            //}
            return NotFound();
        }
    }
}
