using Authentication.Contract.Constants;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using MusicRecommendation.Domain.Repositories;

namespace MusicRecommendation.API.Controllers
{
    public class RecommendationController : BaseController
    {
        private readonly ITrackRecommendationRepository _trackRecommendationRepository;
        private readonly ICurrentUserService _currentUserService;
        public RecommendationController(ILogger<RecommendationController> logger, ITrackRecommendationRepository trackRecommendationRepository, ICurrentUserService currentUserService)
            : base(logger)
        {
            _trackRecommendationRepository = trackRecommendationRepository;
            _currentUserService = currentUserService;
        }


        [HttpGet("user")]
        [AuthFilter(Roles.User)]
        public async Task<IActionResult> GetReccomendationsToUser(int page =1, int size=10)
        {
            var user = await _currentUserService.GetCurrentUserAsync();
            var result = await _trackRecommendationRepository.GetContentBasedRecommendations(user.UserId.Value, page, size);
            return Ok(result);
        }
    }
}
