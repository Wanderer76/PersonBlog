using Conference.Domain.Models;
using Conference.Domain.Services;
using Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conference.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConferenceRoomController : BaseController
    {
        private readonly ILogger<ConferenceRoomController> _logger;
        private readonly IConferenceRoomService _conferenceRoomService;

        public ConferenceRoomController(ILogger<ConferenceRoomController> logger, IConferenceRoomService conferenceRoomService)
            : base(logger)
        {
            _logger = logger;
            _conferenceRoomService = conferenceRoomService;
        }

        [HttpPost("createConferenceToPost")]
        [Produces<ConferenceViewModel>]
        [Authorize]
        public async Task<IActionResult> Index(Guid postId)
        {
            var result = await _conferenceRoomService.CreateConferenceRoomAsync(Guid.Parse(GetUserSession()!), postId);
            return Ok(result);
        }

        [HttpGet("joinLink")]
        [Produces<ConferenceViewModel>]
        public async Task<IActionResult> GetConferenceRoomAsync(Guid roomId)
        {
            var result = await _conferenceRoomService.GetConferenceRoomByIdAsync(roomId);
            return Ok(result);
        }
    }
}
