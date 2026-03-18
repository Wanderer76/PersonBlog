using Infrastructure.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared.Models;

namespace Infrastructure.Models
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        protected readonly ILogger<BaseApiController> _logger;

        protected BaseApiController(ILogger<BaseApiController> logger)
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

        protected ActionResult<T> ToActionResult<T>(Result<T> result)
        {
            if (result.IsSuccess) return Ok(result.Value);

            return result.Errors.Any(e => e.Key == "NotFound")
                ? NotFound(result.Errors)
                : BadRequest(result.Errors.ToValidationProblem());
        }

        protected ActionResult ToActionResult(Result result)
        {
            if (result.IsSuccess) return Ok();

            return result.Errors.Any(e => e.Key == "NotFound")
                ? NotFound(result.Errors)
                : BadRequest(result.Errors.ToValidationProblem());
        }
    }
}
