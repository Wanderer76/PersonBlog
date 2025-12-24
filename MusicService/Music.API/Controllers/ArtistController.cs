using Authentication.Contract.Constants;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Music.Contract.Models.Artist;
using Music.Domain.Services;
using Shared.Services;

namespace Music.API.Controllers
{
    public class ArtistController : BaseApiController
    {
        private readonly IArtistService _artistService;
        private readonly IAvatarService _avatarService;
        private readonly IArtistSearchService _artistSearchService;
        public ArtistController(ILogger<BaseApiController> logger, IArtistService artistService, IAvatarService avatarService, IArtistSearchService artistSearchService)
            : base(logger)
        {
            _artistService = artistService;
            _avatarService = avatarService;
            _artistSearchService = artistSearchService;
        }

        [HttpPost("uploadAvatar")]
        [Produces<Guid>]
        public async Task<IActionResult> UploadAvatar([FromForm] AvatarUploadForm form)
        {
            var avatar = form.Avatar;
            var metadata = new Shared.Models.FileMetadata
            {
                Id = GuidService.GetNewGuid(),
                ContentType = avatar.ContentType,
                CreatedAt = DateTimeService.Now(),
                FileExtension = Path.GetExtension(avatar.FileName),
                Length = avatar.Length,
                Name = avatar.Name,
                ObjectName = avatar.FileName,
            };
            var result = await _avatarService.UploadAvatarAsync(metadata, avatar.OpenReadStream());
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }


        [HttpGet("my")]
        [AuthFilter(Roles.Artist)]
        [Produces<ArtistDetailView>]
        public async Task<IActionResult> GetUserArtist()
        {
            var result = await _artistService.GetCurrentUserArtistAsync();

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        [HttpPost("create")]
        [AuthFilter(Roles.User)]
        public async Task<IActionResult> CrateArtist(CreateArtistRequest createArtistRequest)
        {
            var result = await _artistService.CreateNewArtistAsync(createArtistRequest);

            if (result.IsSuccess)
            {
                return Ok();
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        [HttpGet("search")]
        [Produces<IReadOnlyList<ArtistInfo>>]
        public async Task<IActionResult> SearchArtistByName(string name)
        {
            var result = await _artistSearchService.SearchArtistByName(name);
            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return BadRequest(result.Errors);
        }


    }

    public class AvatarUploadForm
    {
        public IFormFile Avatar { get; set; }
    }
}
