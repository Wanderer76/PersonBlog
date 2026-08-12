using Authentication.Contract.Constants;
using Blog.Contracts.Models.Upload;
using Blog.Contracts.Services;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Blog.API.Controllers;

public sealed class VideoUploadController(
    ILogger<BaseApiController> _logger,
    IMultipartFileUpload _multipartFileUpload,
    ICurrentUserService _currentUserService,
    IVideoService videoService
    ) : BaseApiController(_logger)
{

    [HttpPost("initiate")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<MultipartUploadSession>> InitiateUpload([FromBody] InitiateUploadRequest request)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var objectName = $"{request.PostId}/{request.ObjectName}";

        request.ObjectName = objectName;
        var initResult = await videoService.InitVideoUploadAsync(request);
        if (initResult.IsFailure)
        {
            if (initResult.Errors.Any(error => error.Key == "NotFound"))
                return NotFound(initResult.Errors);

            if (initResult.Errors.Any(error => error.Key == "Forbidden"))
                return Forbid();

            return BadRequest(initResult.Errors);
        }

        var session = await _multipartFileUpload.InitiateUploadAsync(
            user.BlogId.ToString(),
            objectName,
            request.Size);

        return Ok(session);
    }

    [HttpPost("generate-url")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<PreSignedUrl>> GenerateUploadUrl(
        [FromBody] GenerateUrlRequest request)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var bucketId = user.BlogId;

        var url = await _multipartFileUpload.GenerateUploadPartUrlAsync(
            bucketId.ToString(),
            request.UploadId,
            request.PartNumber,
            TimeSpan.FromMinutes(request.ExpiryMinutes));

        return Ok(url);
    }

    [HttpPost("complete")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<string>> CompleteUpload(
        [FromBody] CompleteUploadRequest request)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var bucketId = user.BlogId;

        var session = await _multipartFileUpload.GetUploadSessionAsync(bucketId.ToString(), request.UploadId);
        if (session == null)
        {
            return NotFound();
        }

        if (!TryGetPostId(session, out var postId))
        {
            return BadRequest("Upload session is not associated with a post.");
        }

        var eTag = await _multipartFileUpload.CompleteUploadAsync(
            bucketId.ToString(),
            request.UploadId,
            request.Parts);

        var completeResult = await videoService.CompleteUploadAsync(postId);
        if (completeResult.IsFailure)
        {
            if (completeResult.Errors.Any(error => error.Key == "NotFound"))
                return NotFound(completeResult.Errors);

            if (completeResult.Errors.Any(error => error.Key == "Forbidden"))
                return Forbid();

            return BadRequest(completeResult.Errors);
        }

        return Ok(eTag);
    }

    [HttpPost("abort")]
    [AuthFilter(Roles.Blogger)]
    public async Task<IActionResult> AbortUpload(
        [FromBody] AbortUploadRequest request)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var bucketId = user.BlogId;

        await _multipartFileUpload.AbortUploadAsync(bucketId.ToString(), request.UploadId);
        return Ok();
    }

    [HttpGet("session/{uploadId}")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<MultipartUploadSession>> GetSession(string uploadId)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var bucketId = user.BlogId;

        var session = await _multipartFileUpload.GetUploadSessionAsync(bucketId.ToString(), uploadId);

        if (session == null)
            return NotFound();

        return Ok(session);
    }

    [HttpGet("parts/{uploadId}")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<List<MultipartUploadPart>>> GetParts(string uploadId)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var bucketId = user.BlogId;

        var parts = await _multipartFileUpload.ListPartsAsync(bucketId.ToString(), uploadId);
        return Ok(parts);
    }

    private static bool TryGetPostId(MultipartUploadSession session, out Guid postId)
    {
        var separatorIndex = session.ObjectName.IndexOf('/');
        var postIdSegment = separatorIndex >= 0
            ? session.ObjectName[..separatorIndex]
            : session.ObjectName;

        return Guid.TryParse(postIdSegment, out postId);
    }
}

public class ResumeUploadResponse
{
    public MultipartUploadSession Session { get; set; } = null!;
    public List<MultipartUploadPart> UploadedParts { get; set; } = new();
    public List<int> MissingParts { get; set; } = new();
    public int Progress { get; set; }
}
