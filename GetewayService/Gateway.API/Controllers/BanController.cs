using Authentication.Contract.Constants;
using Blog.Service.Models.File;
using Gateway.API.Api;
using Infrastructure.Extensions;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Profile.Domain.Models;

namespace Gateway.API.Controllers;

public class BanController : BaseApiController
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICurrentUserService _currentUserService;

    public BanController(ILogger<BaseApiController> logger, IHttpClientFactory httpClientFactory, ICurrentUserService currentUserService) : base(logger)
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
            return BadRequest(blogClient.Errors);
        }

        var requestBody = new PostReport
        {
            Message = postReport.Message,
            PostId = postReport.PostId,
            ObjectName = blogClient.Value.ObjectName,
            ReasonId = postReport.ReasonId,
            UserId = user.UserId
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
