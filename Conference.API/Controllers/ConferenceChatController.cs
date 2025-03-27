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
        public ConferenceChatController(ILogger<BaseController> logger, IConferenceChatService conferenceChatService) : base(logger)
        {
            _conferenceChatService = conferenceChatService;
        }

        [HttpPost("sendMessage")]
        [Authorize]
        public async Task SendMessage([FromBody] CreateMessageForm messageForm)
        {
            var sessionId = Guid.Parse(GetUserSession()!);
            var result = await _conferenceChatService.CreateMessageAsync(sessionId, messageForm);
            _hubContext.Clients.Group(messageForm.ConferenceId.ToString()).OnMessageSend(result);
        }

        [HttpGet("messages/{conferenceId:guid}")]
        public async Task<IActionResult> GetLastMessages(Guid conferenceId, int count)
        {
            var result = await _conferenceChatService.GetLastMessagesAsync(conferenceId, count);
            return Ok(result);
        }



    }
}
