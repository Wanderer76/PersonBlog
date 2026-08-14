using Authentication.Contract.Constants;
using Authentication.Contract.Models;
using Blog.Contracts;
using Blog.Contracts.Models;
using Blog.Contracts.Models.Blog;
using Infrastructure.Extensions;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace Gateway.API.Controllers.Blog;

public sealed class BlogController : BaseApiController
{
    private readonly BlogApiClient _blogClient;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHttpClientFactory _httpClientFactory;
    public BlogController(
        ILogger<BaseApiController> logger,
        BlogApiClient blogClient,
        ICurrentUserService currentUserService,
        IHttpClientFactory httpClientFactory) : base(logger)
    {
        _blogClient = blogClient;
        _currentUserService = currentUserService;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Проверка наличия блога у пользователя по ID
    /// </summary>
    [HttpGet("hasBlog/{userId:guid}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<bool>> HasUserBlogById(Guid userId)
    {
        var result = await _blogClient.HasUserBlogAsync(userId);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Errors.ToValidationProblem());
    }

    /// <summary>
    /// Проверка наличия блога у текущего пользователя
    /// </summary>
    [HttpGet("hasUserBlog")]
    [ProducesResponseType(typeof(HasBlogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [AuthFilter(Roles.User)]
    public async Task<ActionResult<HasBlogResponse>> HasUserBlog()
    {
        var user = await _currentUserService.GetCurrentUserAsync();

        var result = await _blogClient.HasUserBlogAsync(user.UserId);

        return result.IsSuccess
            ? Ok(new HasBlogResponse { HasBlog = result.Value })
            : BadRequest(result.Errors.ToValidationProblem());
    }

    /// <summary>
    /// Получение блога текущего пользователя
    /// </summary>
    [HttpGet("detail")]
    [ProducesResponseType(typeof(BlogModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BlogModel>> GetBlogDetail()
    {
        var user = await _currentUserService.GetCurrentUserAsync();

        var result = await _blogClient.GetBlogByUserIdAsync(user.UserId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.Errors.Any(e => e.Key == "NotFound")
                ? NotFound(result.Errors)
                : BadRequest(result.Errors.ToValidationProblem());
    }

    /// <summary>
    /// Получение списка уровней подписки для создания
    /// </summary>
    [HttpGet("subscriptionLevelCreate")]
    [AuthFilter(Roles.Blogger)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSubscriptionLevels()
    {
        var result = await _blogClient.GetAllSubscriptionsAsync();

        return result.IsSuccess
            ? Ok(new { SubscriptionLevels = result.Value })
            : BadRequest(result.Errors.ToValidationProblem());
    }

    /// <summary>
    /// Создание уровня подписки
    /// </summary>
    [HttpPost("subscriptionLevelCreate")]
    [AuthFilter(Roles.Blogger)]
    [ProducesResponseType(typeof(SubscriptionLevelModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SubscriptionLevelModel>> CreateSubscriptionLevel(
        [FromBody] SubscriptionCreateDto form)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _blogClient.CreateSubscriptionAsync(form);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Errors.ToValidationProblem());
    }

    /// <summary>
    /// Получение информации о блоге по ID поста (публичный доступ)
    /// </summary>
    [HttpGet("blogByPost/{postId:guid}")]
    [ProducesResponseType(typeof(BlogModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BlogModel>> GetBlogInfoByPostId(Guid postId)
    {
        var result = await _blogClient.GetBlogByPostIdAsync(postId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.Errors.Any(e => e.Key == "NotFound")
                ? NotFound(result.Errors)
                : BadRequest(result.Errors.ToValidationProblem());
    }

    /// <summary>
    /// Получение информации о блоге для зрителя по ID поста с опциональной авторизацией
    /// </summary>
    [HttpGet("blogViewerInfoByPost/{postId:guid}")]
    [ProducesResponseType(typeof(BlogUserInfoViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BlogUserInfoViewModel>> GetBlogViewerInfoByPostId(Guid postId)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        Guid? viewerId = user.IsAnonymous ? null : user.UserId;
        var result = await _blogClient.GetBlogByPostIdAsync(postId, viewerId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.Errors.Any(e => e.Key == "NotFound")
                ? NotFound(result.Errors)
                : BadRequest(result.Errors.ToValidationProblem());
    }

    /// <summary>
    /// Получение блога по ID
    /// </summary>
    [HttpGet("blog/{blogId:guid}")]
    [ProducesResponseType(typeof(BlogModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BlogModel>> GetBlogById(Guid blogId)
    {
        var result = await _blogClient.GetBlogByIdAsync(blogId);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.Errors.Any(e => e.Key == "NotFound")
                ? NotFound(result.Errors)
                : BadRequest(result.Errors.ToValidationProblem());
    }

    /// <summary>
    /// Создание нового блога
    /// </summary>
    [HttpPost("create")]
    [AuthFilter(Roles.User)]
    [ProducesResponseType(typeof(BlogModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BlogModel>> CreateBlog([FromForm] BlogCreateRequest form)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _blogClient.CreateBlogAsync(form);

        if (result.IsFailure)
        {
            return BadRequest(result.Errors.ToValidationProblem());
        }

        var authClient = _httpClientFactory.CreateClient("Auth");
        var authResponse = await authClient.PostAsJsonAsync(
            "Auth/blog-context",
            new BlogContextRequest(result.Value.Id));

        if (!authResponse.IsSuccessStatusCode)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new ProblemDetails
                {
                    Title = "Не удалось обновить пользовательскую сессию",
                    Detail = "Блог создан, но AuthService не подтвердил обновление контекста пользователя.",
                    Status = StatusCodes.Status502BadGateway
                });
        }

        return Ok(result.Value);
    }

    ///// <summary>
    ///// Обновление блога
    ///// </summary>
    //[HttpPut("{blogId:guid}")]
    //[AuthFilter(Roles.Blogger)]
    //[ProducesResponseType(typeof(BlogModel), StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
    //[ProducesResponseType(StatusCodes.Status403Forbidden)]
    //[ProducesResponseType(StatusCodes.Status404NotFound)]
    //public async Task<ActionResult<BlogModel>> UpdateBlog(
    //    Guid blogId,
    //    [FromBody] BlogUpdateRequest form)
    //{
    //    if (!ModelState.IsValid)
    //    {
    //        return BadRequest(ModelState);
    //    }

    //    var result = await _blogClient.UpdateBlogAsync(blogId, form);

    //    return result.IsSuccess
    //        ? Ok(result.Value)
    //        : result.Errors.Any(e => e.Code == "NotFound")
    //            ? NotFound(result.Errors)
    //            : BadRequest(result.Errors);
    //}

    ///// <summary>
    ///// Удаление блога
    ///// </summary>
    //[HttpDelete("{blogId:guid}")]
    //[AuthFilter(Roles.Blogger)]
    //[ProducesResponseType(StatusCodes.Status204NoContent)]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
    //[ProducesResponseType(StatusCodes.Status403Forbidden)]
    //[ProducesResponseType(StatusCodes.Status404NotFound)]
    //public async Task<IActionResult> DeleteBlog(Guid blogId)
    //{
    //    var result = await _blogClient.DeleteBlogAsync(blogId);

    //    return result.IsSuccess
    //        ? NoContent()
    //        : result.Errors.Any(e => e.Code == "NotFound")
    //            ? NotFound(result.Errors)
    //            : BadRequest(result.Errors);
    //}
}
