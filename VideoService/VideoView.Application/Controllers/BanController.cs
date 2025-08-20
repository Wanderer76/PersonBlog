using Authentication.Contract.Constants;
using Blog.Service.Models.File;
using Infrastructure.Extensions;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Profile.Domain.Models;
using VideoView.Application.Api;

namespace VideoView.Application.Controllers;

public class BanController : BaseController
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICurrentUserService _currentUserService;

    public BanController(ILogger<BaseController> logger, IHttpClientFactory httpClientFactory, ICurrentUserService currentUserService) : base(logger)
    {
        _httpClientFactory = httpClientFactory;
        _currentUserService = currentUserService;
    }

    [HttpPost("sendPostBanRequest")]
    [Authorize]
    [AuthFilter(Roles.User)]
    public async Task<IActionResult> SendBanRequest([FromBody] PostReportForm postReport)
    {
        var user = await _currentUserService.GetCurrentUserAsync();

        var blogClient = await _httpClientFactory.GetFileMetadataAsync(postReport.PostId);

        if (blogClient.IsFailure)
        {
            return BadRequest(blogClient.Error);
        }

        var requestBody = new PostReport
        {
            Message = postReport.Message,
            PostId = postReport.PostId,
            ObjectName = blogClient.Value.ObjectName,
            ReasonId = postReport.ReasonId,
            UserId = user.UserId.Value
        };

        using var client = _httpClientFactory.CreateClientContextHeaders("Reacting", HttpContext);

        var result = await client.PostAsJsonAsync("Ban/sendPostBanRequest", requestBody);

        if (result.IsSuccessStatusCode)
        {
            return Ok();
        }
        else
        {
            return BadRequest(result.Content);
        }
    }
}

public class PostReportForm
{
    public Guid PostId { get; set; }
    public string Message { get; set; }
    public Guid ReasonId { get; set; }
}
