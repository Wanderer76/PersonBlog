using Authentication.Contract.Constants;
using Blog.Contracts;
using Blog.Contracts.Models.Upload;
using Blog.Contracts.Services;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Controllers;

public sealed class VideoUploadController(
    ILogger<BaseApiController> logger,
    VideoUploadApiClient videoUploadApiClient) : BaseApiController(logger)
{
    [HttpPost("initiate")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<MultipartUploadSession>> InitiateUpload(InitiateUploadRequest request)
    {
        return Ok(await videoUploadApiClient.InitiateUploadAsync(request));
    }

    [HttpPost("generate-url")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<PreSignedUrl>> GenerateUrl(GenerateUrlRequest request)
    {
        return Ok(await videoUploadApiClient.GenerateUrlAsync(request));
    }

    [HttpPost("complete")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<string?>> CompleteUpload(CompleteUploadRequest request)
    {
        return Ok(await videoUploadApiClient.CompleteUploadAsync(request));
    }

    [HttpPost("abort")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult> AbortUpload(AbortUploadRequest request)
    {
        await videoUploadApiClient.AbortUploadAsync(request);
        return Ok();
    }

    [HttpGet("session/{uploadId}")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<MultipartUploadSession>> GetSession(string uploadId)
    {
        var session = await videoUploadApiClient.GetSessionAsync(uploadId);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpGet("parts/{uploadId}")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<List<MultipartUploadPart>>> GetParts(string uploadId)
    {
        return Ok(await videoUploadApiClient.GetPartsAsync(uploadId));
    }
}
