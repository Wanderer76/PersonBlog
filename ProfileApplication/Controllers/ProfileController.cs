using Microsoft.AspNetCore.Mvc;
using Profile.Service.Models;

namespace ProfileApplication.Controllers;


[ApiController]
public class ProfileController  : ControllerBase
{
    [HttpPost("create")]
    public async Task<IActionResult> CreateProfile([FromBody] ProfileCreateModel profileCreateModel)
    {
        return Ok("success");
    }
}