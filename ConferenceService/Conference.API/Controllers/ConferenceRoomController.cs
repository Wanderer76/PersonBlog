using Conference.Domain.Models;
using Conference.Domain.Services;
using Infrastructure.Interface;
using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conference.API.Controllers
{
    public class ConferenceRoomController : BaseController
    {
        private readonly ILogger<ConferenceRoomController> _logger;
        private readonly IConferenceRoomService _conferenceRoomService;
        private readonly ICurrentUserService _currentUserService;
        public ConferenceRoomController(ILogger<ConferenceRoomController> logger, IConferenceRoomService conferenceRoomService, ICurrentUserService currentUserService)
            : base(logger)
        {
            _logger = logger;
            _conferenceRoomService = conferenceRoomService;
            _currentUserService = currentUserService;
        }

        [HttpPost("createConferenceToPost")]
        [Produces<ConferenceViewModel>]
        [Authorize]
        public async Task<IActionResult> Index(Guid postId)
        {
            var user = await _currentUserService.GetUserSessionAsync();
            var result = await _conferenceRoomService.CreateConferenceRoomAsync(user.UserId.Value, postId);
            return Ok(result);
        }

        [HttpGet("joinLink")]
        [Produces<ConferenceViewModel>]
        public async Task<IActionResult> GetConferenceRoomAsync(Guid roomId)
        {
            var result = await _conferenceRoomService.GetConferenceRoomByIdAsync(roomId);
            return Ok(result);
        }

        [HttpGet("join")]
        public async Task<IActionResult> Join(Guid roomId)
        {
            var user = await _currentUserService.GetUserSessionAsync();
            await _conferenceRoomService.AddParticipantToConferenceAsync(roomId, user.UserId.Value);
            return Ok();
        }
    }
}