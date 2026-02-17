using Authentication.Contract.Constants;
using Blog.Contracts.Events;
using Blog.Contracts.Services;
using Blog.Domain.Entities;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;

namespace Blog.API.Controllers;

public sealed class VideoUploadController(
    ILogger<BaseApiController> _logger,
    IMultipartFileUpload _multipartFileUpload,
    ICurrentUserService _currentUserService,
    IVideoService videoService,
    IReadWriteRepository<IBlogEntity> context
    ) : BaseApiController(_logger)
{

    [HttpPost("initiate")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<MultipartUploadSession>> InitiateUpload([FromBody] InitiateUploadRequest request)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var objectName = $"{request.PostId}/{request.ObjectName}";
        var session = await _multipartFileUpload.InitiateUploadAsync(
            user.BlogId.ToString(),
            objectName,
            request.Size);

        request.ObjectName = objectName;
        _ = await videoService.InitVideoUploadAsync(request);
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

        var eTag = await _multipartFileUpload.CompleteUploadAsync(
            bucketId.ToString(),
            request.UploadId,
            request.Parts);

        await videoService.CompleteUploadAsync(request.PostId);
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
}

public class CompleteUploadRequest
{
    public Guid PostId { get; set; }
    public string UploadId { get; set; } = null!;
    public List<MultipartUploadPart> Parts { get; set; } = new();
}

public class AbortUploadRequest
{
    public string UploadId { get; set; } = null!;
}

public class ResumeUploadResponse
{
    public MultipartUploadSession Session { get; set; } = null!;
    public List<MultipartUploadPart> UploadedParts { get; set; } = new();
    public List<int> MissingParts { get; set; } = new();
    public int Progress { get; set; }
}

public class GenerateUrlRequest
{
    public string UploadId { get; set; } = null!;
    public int PartNumber { get; set; }
    public int ExpiryMinutes { get; set; } = 5;
}