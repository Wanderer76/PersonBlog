using Authentication.Contract.Constants;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using PlayListService.Contract;
using PlayListService.Services.Services;
using Profile.Service.HttpClients;

namespace Gateway.API.Controllers
{
    public class ProfileController : BaseApiController
    {
        private readonly ProfileHttpClient _profileHttpClient;
        private readonly IPlayListService _playlistHttpApiClient;
        public ProfileController(ILogger<BaseApiController> logger, ProfileHttpClient profileHttpClient, IPlayListService playlistHttpApiClient) : base(logger)
        {
            _profileHttpClient = profileHttpClient;
            _playlistHttpApiClient = playlistHttpApiClient;
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

        [HttpGet("playLists")]
        [AuthFilter(Roles.User)]
        public async Task<IActionResult> GetUserPlayLists()
        {
            return Ok(await _playlistHttpApiClient.GetUserPlayLists());
        }
    }
}
