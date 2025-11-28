using Authentication.Contract.Constants;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Profile.Domain.Models.Profile;
using Profile.Service.HttpClients;
using System.Net.Http;
using System.Text.Json;

namespace Gateway.API.Controllers
{
    public class ProfileController : BaseController
    {
        private readonly ProfileHttpClient _profileHttpClient;
        public ProfileController(ILogger<BaseController> logger, ProfileHttpClient profileHttpClient) : base(logger)
        {
            _profileHttpClient = profileHttpClient;
        }

        [HttpGet("my")]
        [AuthFilter(Roles.User)]
        public async Task<IActionResult> GetMyProfileAsync()
        {
            var result = await _profileHttpClient.GetMyProfileAsync();
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            else
            {
                return Forbid();
            }
        }
    }
}
