//using Authentication.Domain.Interfaces;
//using Infrastructure.Cache.Services;
//using Infrastructure.Models;
//using Microsoft.AspNetCore.Mvc;

//namespace VideoView.Application.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class AuthController : BaseController
//    {
//        private readonly ICacheService _cacheService;
//        private readonly IUserSession _userSession;
//        private readonly IHttpClientFactory _httpClientFactory;

//        //public AuthController(ILogger<AuthController> logger, ICacheService cacheService, IUserSession userSession, IHttpClientFactory httpClientFactory)
//        //: base(logger)
//        //{
//        //    _cacheService = cacheService;
//        //    _userSession = userSession;
//        //    _httpClientFactory = httpClientFactory;
//        //}

//        //[HttpGet("session")]
//        //public async Task<IActionResult> CreateSession()
//        //{
//        //    var session = GetUserSession();
//        //    await _userSession.UpdateUserSession(session);
//        //    return Ok();
//        //}

//        //[HttpPost("create")]
//        //public async Task<IActionResult> CreateUser(RegisterModel registerModel)
//        //{
//        //}

//        //[HttpPost("login")]
//        //[Produces(typeof(AuthResponse))]
//        //public async Task<IActionResult> Login(LoginModel loginModel)
//        //{
//        //    var client = _httpClientFactory.CreateClient();
//        //    var hasSession = Request.Cookies.TryGetValue("sessionId", out var session);
//        //    var response = await client.PostAsJsonAsync("",loginModel);
//        //    await _userSession.UpdateUserSession(session);
//        //    return Ok(response);
//        //}


//        //[HttpPost("refresh")]
//        //[Produces(typeof(AuthResponse))]
//        //public async Task<IActionResult> Refresh(string refreshToken)
//        //{
//        //    var hasSession = Request.Cookies.TryGetValue("sessionId", out var session);
//        //    var response = await _authService.Refresh(refreshToken);
//        //    await _userSession.UpdateUserSession(session);
//        //    return Ok(response);
//        //}
//    }
//}
