using Authentication.Contract.Constants;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Music.Contract.Models;
using Music.Domain.Services;

namespace Music.API.Controllers
{
    public class TrackController : BaseApiController
    {
        private readonly ITrackService _trackService;
        private readonly IGenreService _genreService;
        private readonly IAudioExtractorService _audioExtractorService;

        public TrackController(ILogger<TrackController> logger, ITrackService trackService, IGenreService genreService, IAudioExtractorService audioExtractorService)
            : base(logger)
        {
            _trackService = trackService;
            _genreService = genreService;
            _audioExtractorService = audioExtractorService;
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
            return BadRequest(result.Errors);
        }


        [HttpPost("uploadTrackFile")]
        [AuthFilter(Roles.Artist, Roles.User)]
        [Produces<TrackFileMetadata>]
        public async Task<IActionResult> UploadTrackFile([FromForm] UploadFileForm form)
        {
            if (!IsValidMp3File(form.Track))
            {
                return BadRequest("Error file format");
            }
            var metadata = await _audioExtractorService.ExtractMetadataAsync(form.Track);

            var result = await _trackService.UploadTrackFileAsync(new UploadTrackFile
            {
                ContentType = form.Track.ContentType,
                FileExtension = form.Track.Name,
                Name = form.Track.Name,
                Length = form.Track.Length,
                ObjectName = form.Track.FileName,
                Stream = form.Track.OpenReadStream(),
                Duration = metadata.Duration
            }, metadata);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            return BadRequest(result.Errors);
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
            return BadRequest(result.Errors);
        }

        private bool IsValidMp3File(IFormFile file)
        {
            var allowedExtensions = new[] { ".mp3", ".MP3" };
            var extension = Path.GetExtension(file.FileName);

            return allowedExtensions.Contains(extension) &&
                   file.ContentType.Contains("audio/mpeg");
        }
    }

}
