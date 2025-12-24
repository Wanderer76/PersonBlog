using Conference.Domain.Models;
using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Controllers
{
    public class ConferenceRoomController : BaseApiController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public ConferenceRoomController(ILogger<ConferenceRoomController> logger, IHttpClientFactory httpClientFactory) : base(logger)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("createConferenceToPost")]
        [Authorize]
        public async Task<IActionResult> Index(Guid postId)
        {
            using var client = _httpClientFactory.CreateClient("Conference");
            var result = await client.PostAsync($"ConferenceRoom/createConferenceToPost?postId={postId}", null);
            if (result.IsSuccessStatusCode)
            {
                return Ok(await result.Content.ReadAsStringAsync());
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpGet("joinLink")]
        public async Task<IActionResult> GetConferenceRoomAsync(Guid roomId)
        {
            using var client = _httpClientFactory.CreateClient("Conference");
            var result = await client.GetFromJsonAsync<ConferenceViewModel>($"ConferenceRoom/joinLink?roomId={roomId}");
            return Ok(result);
        }

        [HttpGet("join")]
        public async Task<IActionResult> Join(Guid roomId)
        {
            using var client = _httpClientFactory.CreateClient("Conference");
            var result = await client.GetAsync($"ConferenceRoom/join?roomId={roomId}");
            if (result.IsSuccessStatusCode)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }
    }
}
