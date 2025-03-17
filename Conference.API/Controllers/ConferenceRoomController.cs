using Conference.Domain.Services;
using Infrastructure.Models;
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

        [HttpGet(Name = "Index")]
        public async Task<IActionResult> Index()
        {
            await _conferenceRoomService.AddParticipantToConferenceAsync(Guid.NewGuid(), Guid.NewGuid());
            return Ok();
        }
    }
}
