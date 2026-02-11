using Authentication.Contract.Constants;
using Blog.Contracts.Models.Blog;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Blog.API.Controllers;

public class BlogV2Controller : BaseApiController
{
    public BlogV2Controller(ILogger<BaseApiController> logger) : base(logger)
    {
    }

    [HttpGet("create")]
    [AuthFilter(Roles.User)]
    public async Task<IActionResult> GetBlogCreateModel()
    {
        return Ok();
    }

    [HttpPost("create")]
    [AuthFilter(Roles.User)]
    public async Task<IActionResult> CreateBlog()
    {
        return Ok();
    }

    [HttpPost("update")]
    [AuthFilter(Roles.Blogger)]
    public async Task<IActionResult> UpdateBlog()
    {
        return Ok();
    }

    [HttpGet("update")]
    [AuthFilter(Roles.Blogger)]
    public async Task<IActionResult> GetUpdateBlogModel()
    {
        return Ok();
    }

    [HttpPost("updateThumbnail")]
    [AuthFilter(Roles.Blogger)]
    public async Task<IActionResult> UpdateThumbnail([FromForm] IFormFile thumbnail)
    {
        return Ok();
    }

    [HttpGet("my")]
    [AuthFilter(Roles.Blogger)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(BlogModel))]
    public async Task<IActionResult> GetCurrentUserBlog()
    {
        return Ok();
    }

    [HttpGet("blogById/{blogId:guid}")]
    [AuthFilter]
    public async Task<IActionResult> GetBlogPublicInfoById(Guid blogId)
    {
        return Ok();
    }
}