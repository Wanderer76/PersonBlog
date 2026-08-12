using Conference.Domain.Models;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Controllers
{
    public class ConferenceChatController : BaseApiController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public ConferenceChatController(ILogger<BaseApiController> logger, IHttpClientFactory httpClientFactory) : base(logger)
        {
            _httpClientFactory = httpClientFactory;
        }

        [AuthFilter]
        [HttpPost("sendMessage")]
        public async Task<IActionResult> SendMessage([FromBody] CreateMessageForm messageForm)
        {
            using var client = _httpClientFactory.CreateClient("Conference");
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
            using var client = _httpClientFactory.CreateClient("Conference");
            var result = await client.GetFromJsonAsync<IReadOnlyList<MessageModel>>($"ConferenceChat/messages/{conferenceId}?offset={offset}&count={count}");
            return Ok(result);
        }
    }
}
