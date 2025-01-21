using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Models
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseController : ControllerBase
    {
        protected readonly ILogger<BaseController> _logger;

        protected BaseController(ILogger<BaseController> logger)
        {
            _logger = logger;
        }

        protected void FillHeadersForVideoStreaming(long startPosition, long originalFileSize, long streamLength, long sendSize, string contentType)
        {
            Response.StatusCode = StatusCodes.Status206PartialContent;
            Response.Headers["Accept-Ranges"] = "bytes";
            Response.Headers["Content-Range"] = $"bytes {startPosition}-{sendSize}/{originalFileSize}";
            Response.Headers["Content-Length"] = $"{streamLength}";
            Response.ContentType = contentType;
        }
    }
}
