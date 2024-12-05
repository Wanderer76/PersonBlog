using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Profile.Service.Interface;
using Profile.Service.Models;
using Shared.Services;

namespace ProfileApplication.Controllers;


[ApiController]
[Route("[controller]/")]
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
        _ = await _profileService.CreateProfileAsync(profileCreateModel);
        return Ok();
    }

    [HttpPost("edit")]
    public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateModel profileUpdateModel)
    {
        return Ok("success");
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
    public async Task<IActionResult> GetProfileById(Guid id) { return Ok("success"); }


    [HttpGet("create")]
    public async Task<IActionResult> GetProfileCreateModel() { return Ok("success"); }

}