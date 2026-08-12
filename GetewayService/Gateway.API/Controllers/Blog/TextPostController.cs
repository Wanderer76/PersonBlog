using Authentication.Contract.Constants;
using Blog.Contracts;
using Blog.Contracts.Services;
using Blog.Contracts.Models.Post;
using Blog.Domain.Entities;
using Infrastructure.Extensions;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Controllers.Blog;

public class TextPostController : BaseApiController
{
    private readonly PostApiClient postApiClient;
    public TextPostController(ILogger<BaseApiController> logger, PostApiClient postApiClient) : base(logger)
    {
        this.postApiClient = postApiClient;
    }

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
