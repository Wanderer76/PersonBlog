using Conference.Domain.Models;
using Conference.Domain.Services;
using Conference.Service.Hubs;
using Infrastructure.Interface;
using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Conference.API.Controllers
{
    public class ConferenceChatController : BaseController
    {
        private readonly IConferenceChatService _conferenceChatService;
        private readonly ICurrentUserService _userSession;
        private readonly IHubContext<ConferenceHub, IConferenceHub> _hubContext;
        public ConferenceChatController(ILogger<ConferenceChatController> logger, IConferenceChatService conferenceChatService, IHubContext<ConferenceHub, IConferenceHub> hubContext, ICurrentUserService userSession) : base(logger)
        {
            _conferenceChatService = conferenceChatService;
            _hubContext = hubContext;
            _userSession = userSession;
        }

        [Authorize]
        [HttpPost("sendMessage")]
        public async Task SendMessage([FromBody] CreateMessageForm messageForm)
        {
            var sessionId = await _userSession.GetUserSessionAsync();
            var result = await _conferenceChatService.CreateMessageAsync(sessionId.UserId.Value, messageForm);
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
