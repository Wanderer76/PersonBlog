using Authentication.Contract.Constants;
using Blog.Contracts;
using Blog.Contracts.Services;
using Blog.Contracts.Models.Post;
using Blog.Domain.Entities;
using Gateway.API.Api;
using Gateway.API.Models.TextPost;
using Infrastructure.Extensions;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Controllers.Blog;

public class TextPostController : GatewayApiController
{
    private readonly PostApiClient postApiClient;
    private readonly TextPostDetailApiClient textPostDetailApiClient;

    public TextPostController(
        ILogger<BaseApiController> logger,
        PostApiClient postApiClient,
        TextPostDetailApiClient textPostDetailApiClient) : base(logger)
    {
        this.postApiClient = postApiClient;
        this.textPostDetailApiClient = textPostDetailApiClient;
    }

    [HttpGet("{postId:guid}")]
    [ProducesResponseType<TextPostDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public Task<IActionResult> GetTextPost(
        Guid postId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            () => textPostDetailApiClient.GetDetailAsync(postId, cancellationToken),
            response => Ok(response),
            cancellationToken,
            "Text post request");

    [HttpPost("{postId:guid}/view")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public Task<IActionResult> RegisterView(
        Guid postId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            () => textPostDetailApiClient.RegisterViewAsync(postId, cancellationToken),
            NoContent,
            cancellationToken,
            "Text post view registration");

    [HttpGet("create")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<CreatePostModelViewModel>> GetPostCreateModel()
    {
        var model = await postApiClient.GetPostCreateModelAsync();
        return Ok(model);
    }

    [HttpPost("createTextPost")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<UserPostInfoModel>> CreateTextPost([FromForm] TextPostCreateForm textPostCreateForm)
    {
        var postCreateResult = await postApiClient.CreateTextPostAsync(textPostCreateForm);
        return ToActionResult(postCreateResult);
    }

    [HttpGet("edit/{postId:guid}")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<TextPostEditViewModel>> GetTextPostEditModel(Guid postId)
    {
        return ToActionResult(await postApiClient.GetTextPostEditModelAsync(postId));
    }

    [HttpPost("edit")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult> EditTextPost([FromForm] TextPostEditDto request)
    {
        return ToActionResult(await postApiClient.UpdateTextPostAsync(request));
    }

}
