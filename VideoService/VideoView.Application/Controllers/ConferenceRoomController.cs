using Blog.Domain.Entities;
using Conference.Domain.Models;
using Infrastructure.Interface;
using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Utils;

namespace VideoView.Application.Controllers
{
    public class ConferenceRoomController : BaseController
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
            using var client = _httpClientFactory.CreateClientContextHeaders("Conference", HttpContext);
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
            using var client = _httpClientFactory.CreateClientContextHeaders("Conference", HttpContext);
            var result = await client.GetFromJsonAsync<ConferenceViewModel>($"ConferenceRoom/joinLink?roomId={roomId}");
            return Ok(result);
        }

        [HttpGet("join")]
        public async Task<IActionResult> Join(Guid roomId)
        {
            using var client = _httpClientFactory.CreateClientContextHeaders("Conference", HttpContext);
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
