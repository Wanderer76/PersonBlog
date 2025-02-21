using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using System;

namespace VideoView.Application.Controllers;

//// VideoController.cs
//[ApiController]
//[Route("api/videos")]
//public class VideoController : ControllerBase
//{
//    private readonly AppDbContext _context;
//    private readonly IDatabase _redis;

//    public VideoController(AppDbContext context, IConnectionMultiplexer redis)
//    {
//        _context = context;
//        _redis = redis.GetDatabase();
//    }

//    [HttpPost("{id}/view")]
//    [ServiceFilter(typeof(UserAgentFilter))]
//    [ServiceFilter(typeof(BehaviorAnalysisFilter))]
//    public async Task<IActionResult> TrackView(int id, [FromBody] ViewRequest request)
//    {
//        // Проверка reCAPTCHA
//        if (request.IsSuspicious && !await VerifyRecaptcha(request.Token))
//            return Forbid();

//        // Проверка уникальности просмотра
//        var sessionKey = $"video:{id}:views:{request.SessionHash}";
//        if (await _redis.KeyExistsAsync(sessionKey))
//            return Ok(new { Views = await GetCurrentViews(id) });

//        // Обновление счетчика
//        await _context.Videos
//            .Where(v => v.Id == id)
//            .ExecuteUpdateAsync(v => v.SetProperty(x => x.Views, x => x.Views + 1));

//        await _redis.StringSetAsync(sessionKey, "1", TimeSpan.FromDays(1));

//        return Ok(new { Views = await GetCurrentViews(id) });
//    }

//    private async Task<int> GetCurrentViews(int videoId)
//    {
//        return await _context.Videos
//            .Where(v => v.Id == videoId)
//            .Select(v => v.Views)
//            .FirstOrDefaultAsync();
//    }

//    private async Task<bool> VerifyRecaptcha(string token)
//    {
//        // Реализация проверки через RecaptchaService
//    }
//}

//// ViewRequest.cs
//public class ViewRequest
//{
//    public string SessionHash { get; set; }
//    public string Token { get; set; }
//    public bool IsSuspicious { get; set; }
//}


// UserAgentAnalysisMiddleware.cs
//public class UserAgentAnalysisMiddleware
//{
//    private readonly RequestDelegate _next;
//    private readonly List<string> _botPatterns;

//    public UserAgentAnalysisMiddleware(RequestDelegate next)
//    {
//        _next = next;
//        _botPatterns = new List<string> { "bot", "crawler", "spider", "curl", "wget" };
//    }

//    public async Task InvokeAsync(HttpContext context)
//    {
//        var userAgent = context.Request.Headers.UserAgent.ToString();
//        var parser = Parser.GetDefault();
//        var clientInfo = parser.Parse(userAgent);

//        var isBot = _botPatterns.Any(p => userAgent.Contains(p, StringComparison.OrdinalIgnoreCase)) ||
//                   clientInfo.Device.IsSpider;

//        context.Items["IsSuspiciousUA"] = isBot;
//        context.Items["DeviceType"] = clientInfo.Device.Family;
//        context.Items["Browser"] = clientInfo.UA.Family;

//        await _next(context);
//    }
//}

//// BehaviorAnalysisService.cs
//public class BehaviorAnalysisService
//{
//    private readonly IDatabase _redis;
//    private readonly IConnectionMultiplexer _redisConnection;

//    public BehaviorAnalysisService(IConnectionMultiplexer redisConnection)
//    {
//        _redisConnection = redisConnection;
//        _redis = redisConnection.GetDatabase();
//    }

//    public async Task<bool> AnalyzeRequestAsync(string ip)
//    {
//        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
//        var key = $"behavior:{ip}";

//        // Получаем историю запросов
//        var history = (await _redis.ListRangeAsync(key))
//            .Select(v => long.Parse(v))
//            .ToList();

//        // Добавляем текущий запрос
//        await _redis.ListLeftPushAsync(key, now.ToString());
//        await _redis.KeyExpireAsync(key, TimeSpan.FromMinutes(10));

//        // Анализ паттернов
//        var recent = history.Where(t => now - t < 300000).ToList();
//        if (recent.Count > 15) return true; // Более 15 запросов за 5 мин

//        var timeDiffs = recent.Zip(recent.Skip(1), (a, b) => a - b);
//        var avgDiff = timeDiffs.Any() ? timeDiffs.Average() : 0;

//        return avgDiff < 1000 && recent.Count > 5;
//    }
//}

//// BehaviorAnalysisMiddleware.cs
//public class BehaviorAnalysisMiddleware
//{
//    private readonly RequestDelegate _next;

//    public BehaviorAnalysisMiddleware(RequestDelegate next)
//    {
//        _next = next;
//    }

//    public async Task InvokeAsync(HttpContext context, BehaviorAnalysisService service)
//    {
//        var ip = context.Connection.RemoteIpAddress?.ToString();

//        if (!string.IsNullOrEmpty(ip))
//        {
//            var isSuspicious = await service.AnalyzeRequestAsync(ip);
//            if (isSuspicious)
//            {
//                context.Response.StatusCode = 429;
//                await context.Response.WriteAsync("Too many requests");
//                return;
//            }
//        }

//        await _next(context);
//    }
//}
