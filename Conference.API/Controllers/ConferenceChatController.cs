using Conference.Domain.Models;
using Conference.Domain.Services;
using Conference.Service.Hubs;
using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Conference.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConferenceChatController : BaseController
    {
        private readonly IConferenceChatService _conferenceChatService;
        private readonly IHubContext<ConferenceHub, IConferenceHub> _hubContext;
        public ConferenceChatController(ILogger<ConferenceChatController> logger, IConferenceChatService conferenceChatService, IHubContext<ConferenceHub, IConferenceHub> hubContext) : base(logger)
        {
            _conferenceChatService = conferenceChatService;
            _hubContext = hubContext;
        }

        [HttpPost("sendMessage")]
        [Authorize]
        public async Task SendMessage([FromBody] CreateMessageForm messageForm)
        {
            var sessionId = Guid.Parse(GetUserSession()!);
            var result = await _conferenceChatService.CreateMessageAsync(sessionId, messageForm);
            await _hubContext.Clients.Group(messageForm.ConferenceId.ToString()).OnMessageSend(result);
        }

        [HttpGet("messages/{conferenceId:guid}")]
        public async Task<IActionResult> GetLastMessages(Guid conferenceId, int offset, int count)
        {
            var result = await _conferenceChatService.GetLastMessagesAsync(conferenceId, offset, count);
            return Ok(result);
        }
    }
}
