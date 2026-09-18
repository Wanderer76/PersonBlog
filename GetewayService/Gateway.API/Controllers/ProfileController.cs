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
using Profile.Application.Models.Profile;
using System.ComponentModel.DataAnnotations;

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

    [HttpPost("edit")]
    [AuthFilter(Roles.User)]
    [ProducesResponseType(typeof(ProfileModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProfileModel>> UpdateMyProfileAsync(
        [FromForm] UpdateMyProfileRequest request,
        CancellationToken cancellationToken)
    {
        var user = await currentUserService.GetCurrentUserAsync();
        var currentProfile = await profileHttpClient.GetMyProfileAsync(cancellationToken);
        if (currentProfile.IsFailure)
            return ToActionResult(currentProfile);

        if (currentProfile.Value.UserId != user.UserId)
            return Forbid();

        var result = await profileHttpClient.UpdateProfileAsync(
            currentProfile.Value.Id,
            user.UserId,
            request.Name.Trim(),
            request.ProfilePicture,
            cancellationToken);

        return ToActionResult(result);
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

public sealed class UpdateMyProfileRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    public IFormFile? ProfilePicture { get; init; }
}

public record UserContext(BlogInfo Blog, Features Features, UserPermissions Permissions);
public record UserPermissions(PermissionItem PublishVideo, PermissionItem PublishText);
public record PermissionItem(bool IsAllowed);
public record Features(bool FriendsEnabled, bool MessagesEnabled);
public record BlogInfo(bool HasBlog, Guid? Id);
