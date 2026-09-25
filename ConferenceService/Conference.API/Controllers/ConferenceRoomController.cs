using Conference.Domain.Models;
using Conference.Domain.Services;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.Middleware;

namespace Conference.API.Controllers
{
    public class ConferenceRoomController : BaseApiController
    {
        private readonly IConferenceRoomService _conferenceRoomService;
        private readonly ICurrentUserService _currentUserService;
        public ConferenceRoomController(ILogger<ConferenceRoomController> logger, IConferenceRoomService conferenceRoomService, ICurrentUserService currentUserService)
            : base(logger)
        {
            _conferenceRoomService = conferenceRoomService;
            _currentUserService = currentUserService;
        }

        [HttpPost("createConferenceToPost")]
        [Produces<ConferenceViewModel>]
        [Authorize]
        public async Task<IActionResult> Index(Guid postId)
        {
            var user = await _currentUserService.GetCurrentUserAsync();
            var result = await _conferenceRoomService.CreateConferenceRoomAsync(user.UserId, postId);
            return Ok(result);
        }

        [HttpPost("{roomId:guid}/invitations")]
        [AuthFilter]
        [Produces<ConferenceInvitationViewModel>]
        public async Task<ActionResult<ConferenceInvitationViewModel>> CreateInvitation(Guid roomId,
            CreateConferenceInvitationRequest request, CancellationToken cancellationToken)
        {
            var result = await _conferenceRoomService.CreateInvitationAsync(roomId, request, cancellationToken);
            return ToActionResult(result);
        }

        [HttpGet("joinLink")]
        [Produces<ConferenceViewModel>]
        public async Task<IActionResult> GetConferenceRoomAsync(Guid roomId)
        {
            var result = await _conferenceRoomService.GetConferenceRoomByIdAsync(roomId);
            return Ok(result);
        }

        [HttpGet("join")]
        [Authorize]
        public async Task<IActionResult> Join(Guid roomId)
        {
            var user = await _currentUserService.GetCurrentUserAsync();
            await _conferenceRoomService.AddParticipantToConferenceAsync(roomId, user.UserId);
            return Ok();
        }
    }
}
