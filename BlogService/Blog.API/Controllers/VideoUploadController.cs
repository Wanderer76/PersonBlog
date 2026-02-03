using Authentication.Contract.Constants;
using Blog.Domain.Entities;
using Blog.Domain.Events;
using Blog.Service.Models.File;
using Blog.Service.Services;
using Blog.Service.Services.Implementation;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;

namespace Blog.API.Controllers;

public class VideoUploadController(
    ILogger<BaseApiController> _logger,
    IMultipartFileUpload _multipartFileUpload,
    ICurrentUserService _currentUserService,
    IVideoService videoService,
    IReadWriteRepository<IBlogEntity> context
    ) : BaseApiController(_logger)
{

    [HttpPost("initiate")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<MultipartUploadSession>> InitiateUpload(
    [FromBody] InitiateUploadRequest request)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var bucketId = user.UserId;

        var metadata = await videoService.CreateFileMetadataAsync(request);
        var session = await _multipartFileUpload.InitiateUploadAsync(
            bucketId.ToString(),
            $"{request.PostId}/{request.ObjectName}",
            request.Size);



        return Ok(session);
    }

    [HttpPost("generate-url")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<PreSignedUrl>> GenerateUploadUrl(
        [FromBody] GenerateUrlRequest request)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var bucketId = user.UserId;

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
        var bucketId = user.UserId;

        var eTag = await _multipartFileUpload.CompleteUploadAsync(
            bucketId.ToString(),
            request.UploadId,
            request.Parts);

        var metadata = await context.Get<VideoFile>()
            .Where(x => x.PostId == request.PostId)
            .FirstAsync();

        var videoCreateEvent = new CombineFileChunksCommand
        {
            BlogId = user.BlogId,
            VideoMetadataId = metadata.Id,
            PostId = metadata.PostId,
        };

        var post = await context.Get<Post>()
            .FirstAsync(x => x.Id == metadata.PostId);

        context.Attach(post);

        var videoEvent = VideoProcessEvent.Create(videoCreateEvent, videoCreateEvent.VideoMetadataId);
        post.ProcessState = ProcessState.Running;
        context.Add(videoEvent);
        await context.SaveChangesAsync();

        return Ok(eTag);
    }

    [HttpPost("abort")]
    [AuthFilter(Roles.Blogger)]
    public async Task<IActionResult> AbortUpload(
        [FromBody] AbortUploadRequest request)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var bucketId = user.UserId;

        await _multipartFileUpload.AbortUploadAsync(bucketId.ToString(), request.UploadId);
        return Ok();
    }

    [HttpGet("session/{uploadId}")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<MultipartUploadSession>> GetSession(string uploadId)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var bucketId = user.UserId;

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
        var bucketId = user.UserId;

        var parts = await _multipartFileUpload.ListPartsAsync(bucketId.ToString(), uploadId);
        return Ok(parts);
    }

    /*
    [HttpPost("uploadChunk")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult> UploadVideoChunk([FromForm] UploadVideoChunkForm uploadVideoChunk)
    {
        try
        {
            var metadata = await videoService.GetOrCreateVideoMetadata(uploadVideoChunk.ToUploadVideoChunkModel());
            using var data = uploadVideoChunk.ChunkData.OpenReadStream();
            await postService.UploadVideoChunkAsync(new UploadVideoChunkDto
            {
                ChunkNumber = uploadVideoChunk.ChunkNumber,
                TotalChunkCount = uploadVideoChunk.TotalChunkCount,
                ChunkData = data,
                PostId = uploadVideoChunk.PostId
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex);
        }
        return Ok();
    }*/
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