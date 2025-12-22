using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Profile.Domain.Models.Profile;
using Profile.Domain.Services;

namespace Profile.API.Controllers;


[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly ICurrentUserService _currentUserService;

    public ProfileController(IProfileService profileService, ICurrentUserService currentUserService)
    {
        _profileService = profileService;
        _currentUserService = currentUserService;
    }

    [HttpPost("edit")]
    [Authorize]
    public async Task<ActionResult<ProfileModel>> UpdateProfile([FromBody] ProfileUpdateModel profileUpdateModel)
    {
        var result = await _profileService.UpdateProfileAsync(profileUpdateModel);
        return Ok(result);
    }

    [HttpGet("profile/my")]
    [Authorize]
    public async Task<ActionResult<ProfileModel>> GetProfile()
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        if (!user.IsAnonymous)
        {
            var profileModel = await _profileService.GetProfileByUserIdAsync(user.UserId);
            return Ok(profileModel);
        }
        return Forbid();
    }

    [HttpGet("profile/{userId:guid}")]
    public async Task<ActionResult<ProfileModel>> GetProfileById(Guid userId)
    {
        var result = await _profileService.GetProfileByUserIdAsync(userId);
        return Ok(result);
    }

    [HttpDelete("delete/{id:guid}")]
    public async Task<IActionResult> DeleteProfile(Guid id)
    {
        await _profileService.DeleteProfileByUserIdAsync(id);
        return Ok();
    }

    [HttpGet("profileId/{id:guid}")]
    [Produces(typeof(Guid?))]
    public async Task<ActionResult<Guid?>> GetProfileIdById(Guid id)
    {
        var result = await _profileService.GetProfileIdByUserIdIfExistsAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Not implemented возможно и не надо
    /// </summary>
    /// <returns></returns>
    [HttpGet("create")]
    public async Task<IActionResult> GetProfileCreateModel() { return Ok("success"); }

}