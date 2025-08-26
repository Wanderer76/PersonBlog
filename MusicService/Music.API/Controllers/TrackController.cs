using Authentication.Contract.Constants;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Music.Contract.Models;
using Music.Domain.Services;

namespace Music.API.Controllers
{
    public class TrackController : BaseController
    {
        private readonly ITrackService _trackService;
        private readonly IGenreService _genreService;

        public TrackController(ILogger<TrackController> logger, ITrackService trackService, IGenreService genreService)
            : base(logger)
        {
            _trackService = trackService;
            _genreService = genreService;
        }


        [HttpGet("create")]
        [AuthFilter(Roles.Artist, Roles.User)]
        [Produces<CreateTrackViewModel>]
        public async Task<IActionResult> CreateTrack()
        {
            var genres = await _genreService.GetGenreListAsync();

            return Ok(new CreateTrackViewModel(
                genres.IsSuccess ? genres.Value : []
            ));
        }

        [HttpPost("create")]
        [AuthFilter(Roles.Artist, Roles.User)]
        public async Task<IActionResult> CreateTrack([FromBody] TrackCreateRequest trackCreate)
        {
            var result = await _trackService.CreateTrackAsync(trackCreate);
            if (result.IsSuccess)
            {
                return Ok();
            }
            return BadRequest(result.Error);
        }


        [HttpPost("uploadTrackFile")]
        [AuthFilter(Roles.Artist, Roles.User)]
        public async Task<IActionResult> UploadTrackFile([FromForm] UploadFileForm form)
        {
            var result = await _trackService.UploadTrackFileAsync(new UploadTrackFile
            {
                ContentType = form.Track.ContentType,
                FileExtension = form.Track.Name,
                Name = form.Track.Name,
                Length = form.Track.Length,
                ObjectName = form.Track.FileName,
                Stream = form.Track.OpenReadStream(),
                Duration = form.Duration
            });
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            return BadRequest(result.Error);
        }

        [HttpPost("uploadTrackThumbnail")]
        [AuthFilter(Roles.Artist, Roles.User)]
        public async Task<IActionResult> UploadTrackThumbnail([FromForm] UploadThumbnailFileForm form)
        {
            var result = await _trackService.UploadThumbnailFileAsync(new UploadThumbnailFile
            {
                ContentType = form.Thumbnail.ContentType,
                FileExtension = form.Thumbnail.Name,
                Name = form.Thumbnail.Name,
                Length = form.Thumbnail.Length,
                ObjectName = form.Thumbnail.FileName,
                Stream = form.Thumbnail.OpenReadStream(),
            });
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            return BadRequest(result.Error);
        }
    }
}
