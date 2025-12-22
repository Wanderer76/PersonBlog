using Blog.Domain.Entities;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;

namespace Blog.API.Controllers.Internal
{
    public class InternalPostController : BaseController
    {
        private readonly IReadRepository<IBlogEntity> _repository;
        public InternalPostController(ILogger<BaseController> logger, IReadRepository<IBlogEntity> repository) : base(logger)
        {
            _repository = repository;
        }

        [HttpGet("blogSubscribers/{blogId}")]
        public async IAsyncEnumerable<string> GetBlogSubscribers(Guid blogId, int page, int pageSize)
        {
            Response.Headers.ContentType = "application/x-ndjson"; // или text/plain
            var query = _repository.Get<Subscriber>()
                .Active()
                .Where(x => x.BlogId == blogId)
                .Select(x => x.UserId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            await foreach (var subscriber in query.AsAsyncEnumerable())
            {
                yield return subscriber.ToString();
            }
        }
    }
}
