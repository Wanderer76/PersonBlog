using Authentication.Contract.Constants;
using Blog.Contracts;
using Blog.Contracts.Models.Blog;
using Infrastructure.Extensions;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using PlayListService.Contract;
using PlayListService.Services.Services;
using Profile.Service.HttpClients;

namespace Gateway.API.Controllers;

public class ProfileController(ILogger<BaseApiController> logger,
    ProfileHttpClient profileHttpClient,
    BlogApiClient blogClient,
    ICurrentUserService currentUserService,
    IPlayListService playlistHttpApiClient) 
    : BaseApiController(logger)
{
    [HttpGet("my")]
    [AuthFilter(Roles.User)]
    public async Task<IActionResult> GetMyProfileAsync()
    {
        var result = await profileHttpClient.GetMyProfileAsync();
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }
        else
        {
            return Forbid();
        }
    }

    [HttpGet("playLists")]
    [AuthFilter(Roles.User)]
    public async Task<IActionResult> GetUserPlayLists()
    {
        return Ok(await playlistHttpApiClient.GetUserPlayLists());
    }

    [HttpGet("context")]
    [AuthFilter(Roles.User)]
    [Produces<UserContext>]
    public async Task<ActionResult<UserContext>> GetProfileContext()
    {
        var user = await currentUserService.GetCurrentUserAsync();
        var blog = await blogClient.HasUserBlogAsync(user.UserId);
        BlogInfo? blogInfo;
        if (blog.IsSuccess)
        {
            blogInfo = new BlogInfo(blog.Value.HasBlog, blog.Value.BlogId);
        }
        else
        {
            return BadRequest(blog.ToValidationProblem());
        }

        var result = new UserContext(
            blogInfo,
            new Features(false, false),
            new UserPermissions(
                new PermissionItem(blogInfo.HasBlog),
                new PermissionItem(blogInfo.HasBlog))
            );

        return Ok(result);
    }
}

public record UserContext(BlogInfo Blog, Features Features, UserPermissions Permissions);
public record UserPermissions(PermissionItem PublishVideo, PermissionItem PublishText);
public record PermissionItem(bool IsAllowed);
public record Features(bool FriendsEnabled, bool MessagesEnabled);
public record BlogInfo(bool HasBlog, Guid? Id);