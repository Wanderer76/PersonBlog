using Conference.Domain.Models;
using Conference.Domain.Services;
using Infrastructure.Extensions;
using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace VideoView.Application.Controllers
{
    public class ConferenceChatController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public ConferenceChatController(ILogger<BaseController> logger, IHttpClientFactory httpClientFactory) : base(logger)
        {
            _httpClientFactory = httpClientFactory;
        }

        [Authorize]
        [HttpPost("sendMessage")]
        public async Task<IActionResult> SendMessage([FromBody] CreateMessageForm messageForm)
        {
            using var client = _httpClientFactory.CreateClientContextHeaders("Conference", HttpContext);
            var result = await client.PostAsJsonAsync($"ConferenceChat/sendMessage", messageForm);
            if (result.IsSuccessStatusCode)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpGet("messages/{conferenceId:guid}")]
        public async Task<IActionResult> GetLastMessages(Guid conferenceId, int offset, int count)
        {
            using var client = _httpClientFactory.CreateClientContextHeaders("Conference", HttpContext);
            var result = await client.GetFromJsonAsync<IReadOnlyList<MessageModel>>($"ConferenceChat/messages/{conferenceId}?offset={offset}&count={count}");
            return Ok(result);
        }
    }
}
