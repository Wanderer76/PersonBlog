//using Authentication.Contract.Constants;
//using Blog.Contracts.Models.Blog;
//using Infrastructure.Middleware;
//using Infrastructure.Models;
//using Microsoft.AspNetCore.Mvc;

//namespace Gateway.API.Controllers;

//public sealed class BlogController(
//    ILogger<BaseApiController> logger) 
//    : BaseApiController(logger)
//{

//    [HttpGet("hasCurrentUserBlog")]
//    [AuthFilter(Roles.User)]
//    public async Task<IActionResult> HasCurrentUserBlog()
//    {
//        return Ok();
//    }

//    [HttpGet("my")]
//    [AuthFilter(Roles.Blogger)]
//    public async Task<IActionResult> GetCurrentUserBlog()
//    {
//        return Ok();
//    }


//    [HttpPost("create")]
//    [AuthFilter(Roles.User)]
//    public async Task<ActionResult<BlogModel>> CreateBlog([FromForm] BlogCreateForm form)
//    {
//    }

//    [HttpPost("update")]
//    [AuthFilter(Roles.Blogger)]
//    public async Task<ActionResult<BlogModel>> UpdateBlog([FromForm] BlogCreateForm form)
//    {
//    }
//}
