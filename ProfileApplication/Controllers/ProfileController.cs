using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Profile.Service.Interface;
using Profile.Service.Models;
using Shared.Services;

namespace ProfileApplication.Controllers;


[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateProfile([FromBody] ProfileCreateModel profileCreateModel)
    {
        var result = await _profileService.CreateProfileAsync(profileCreateModel);
        return Ok(result);
    }

    [HttpPost("edit")]
    [Authorize]
    public async Task<ActionResult<ProfileModel>> UpdateProfile([FromBody] ProfileUpdateModel profileUpdateModel)
    {
        var result = await _profileService.UpdateProfileAsync(profileUpdateModel);
        return Ok(result);
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<ProfileModel>> GetProfile()
    {
        var userId = HttpContext.GetUserFromContext();
        var profileModel = _profileService.GetProfileByUserIdAsync(userId);
        return Ok(await profileModel);
    }

    [HttpGet("profile/{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ProfileModel>> GetProfileById(Guid id) {

        var result = await _profileService.GetProfileByUserIdAsync(id);
        return Ok(result);
    }

    [HttpDelete("delete/{id:guid}")]
    public async Task<IActionResult> DeleteProfile(Guid id)
    {
        await _profileService.DeleteProfileByUserIdAsync(id);
        return Ok();
    }

    /// <summary>
    /// Not implemented возможно и не надо
    /// </summary>
    /// <returns></returns>
    [HttpGet("create")]
    public async Task<IActionResult> GetProfileCreateModel() { return Ok("success"); }

}