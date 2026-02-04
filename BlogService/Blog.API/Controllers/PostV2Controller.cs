using Authentication.Contract.Constants;
using Blog.API.Models;
using Blog.Contracts.Models;
using Blog.Domain.Entities;
using Blog.Domain.Services;
using Blog.Domain.Services.Models;
using Blog.Service.Models.File;
using Blog.Service.Services;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;

namespace Blog.API.Controllers;

public class PostV2Controller(
    ILogger<BaseApiController> logger,
    IReadWriteRepository<IBlogEntity> repository,
    ICurrentUserService currentUserService,
    IFileStorageFactory fileStorageFactory,
    IPostService postService,
    ISubscriptionLevelService subscriptionLevelService,
    ICategoryService categoryService,
    IVideoService videoService)
    : BaseApiController(logger)
{
    [HttpGet("my")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<PagedListViewModel<UserPostInfoDto>>> GetCurrentUserPostPaged(int page, int pageSize, PostType postType = PostType.Video)
    {
        var user = await currentUserService.GetCurrentUserAsync();

        var postQuery = repository.Get<Post>()
            .Where(x => x.IsDelete == false)
            .Where(x => x.BlogId == user.BlogId)
            .Where(x => x.Type == postType);

        var totalCount = await postQuery.CountAsync();

        postQuery = postType == PostType.Video
            ? postQuery
                .Include(x => x.VideoPostInfo).ThenInclude(x => x.PostCategories)
                .Include(x => x.VideoPostInfo).ThenInclude(x => x.VideoFile)
                .Include(x => x.VideoPostInfo).ThenInclude(x => x.PreviewFile)
            : postQuery.Include(x => x.TextPostInfo);

        var postList = await postQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        using var storage = fileStorageFactory.CreateFileStorage();

        var result = postList.Select(async x => new UserPostInfoDto
        {
            Id = x.Id,
            BlogId = x.BlogId,
            DislikeCount = x.DislikeCount,
            LikeCount = x.LikeCount,
            ViewCount = x.ViewCount,
            PaymentSubscriptionId = x.PaymentSubscriptionId,
            Visibility = x.Visibility,
            Title = x.Title,
            CreatedAt = x.CreatedAt,
            TextInfo = postType == PostType.Text ? new TextInfoDto
            {
                Text = x.TextPostInfo.Text,
            } : null,
            VideoInfo = postType == PostType.Video ? new VideoInfoDto
            {
                ProcessState = x.ProcessState,
                PreviewUrl = x.VideoPostInfo.PreviewFile == null ? null : await storage.GetFileUrlAsync(x.BlogId, x.VideoPostInfo.PreviewFile!.ObjectName),
                VideoMetadata = x.IsProcessComplete() ? new VideoMetadataModel(
                                x.VideoPostInfo.VideoFile!.Id,
                                x.VideoPostInfo.VideoFile!.Length,
                                x.VideoPostInfo.VideoFile!.Duration,
                                x.VideoPostInfo.VideoFile!.ContentType,
                                x.VideoPostInfo.VideoFile!.ObjectName
                            ) : null
            } : null
        });

        return Ok(new PagedListViewModel<UserPostInfoDto>((int)Math.Ceiling((double)totalCount / pageSize), pageSize, await Task.WhenAll(result)));
    }

    [HttpGet("create")]
    [AuthFilter(Roles.Blogger)]
    public async Task<IActionResult> GetPostCreateModel()
    {
        var subscriptionLevels = await subscriptionLevelService.GetAllSubscriptionsAsync();
        var visibilityList = await postService.GetPostVisibilityListAsync();
        var categoryList = await categoryService.GetAllCategoriesAsync();
        return Ok(new CreatePostModelViewModel(subscriptionLevels, visibilityList, categoryList));
    }

    [HttpPost("create")]
    [AuthFilter(Roles.Blogger)]
    public async Task<IActionResult> CreatePost([FromBody] PostCreateRequest request)
    {
        var currentUser = await currentUserService.GetCurrentUserAsync();
        var blogId = currentUser.BlogId;

        if (request.Type == PostType.Video && request.VideoPostData == null)
            return BadRequest("VideoPostData is required for video posts.");

        if (request.Type == PostType.Text && request.TextPostData == null)
            return BadRequest("TextPostData is required for text posts.");

        var postId = GuidService.GetNewGuid();

        var description = request.Type == PostType.Video ? request.VideoPostData?.Description : null;
        var text = request.Type == PostType.Text ? request.TextPostData!.Text.Trim() : null;

        var categories = (request.VideoPostData?.Categories) != null
            ? await categoryService.GetCategoriesByIdsAsync(request.VideoPostData.Categories)
            : [];

        var post = new Post(
            id: postId,
            blogId: blogId,
            type: request.Type,
            description: description,
            title: request.Title,
            paymentSubscriptionId: null,
            visibility: request.Visibility,
            categories: request.VideoPostData?.Categories == null ? [] : categories.Select(x => x.Id),
            text: text
        );

        using var storage = fileStorageFactory.CreateFileStorage();

        if (request.Type == PostType.Text && request.TextPostData?.Files != null)
        {
            var uploadedFiles = new List<PostFile>();
            foreach (var file in request.TextPostData.Files)
            {
                if (file.Length == 0) continue;
                var id = GuidService.GetNewGuid();
                var objectName = await storage.PutFileAsync(blogId, $"{postId}/{id}", file.OpenReadStream());
                uploadedFiles.Add(new PostFile
                {
                    Id = id,
                    PostId = postId,
                    ObjectName = objectName,
                    Name = Path.GetFileName(file.FileName),
                    ContentType = file.ContentType,
                    Length = file.Length,
                    CreatedAt = DateTimeService.Now(),
                    FileExtension = Path.GetExtension(file.FileName)
                });
            }
            post.TextPostInfo = new TextPostInfo(postId, text, uploadedFiles);
        }

        if (request.Type == PostType.Video && request.VideoPostData?.Thumbnail != null)
        {
            var thumb = request.VideoPostData.Thumbnail;
            if (thumb.Length > 0)
            {
                var thumbId = GuidService.GetNewGuid();
                var objectName = await storage.PutFileAsync(blogId, $"{postId}/{thumbId}", thumb.OpenReadStream());
                var previewFile = new PostFile
                {
                    Id = thumbId,
                    PostId = postId,
                    ObjectName = objectName,
                    Name = Path.GetFileName(thumb.FileName),
                    FileExtension = Path.GetExtension(thumb.FileName),
                    ContentType = thumb.ContentType,
                    Length = thumb.Length,
                    CreatedAt = DateTimeService.Now(),
                };
                post.VideoPostInfo.PreviewFile = previewFile;
                post.VideoPostInfo.PreviewId = previewFile.Id;
            }
        }
        repository.Add(post);
        await repository.SaveChangesAsync();

        return Ok(new UserPostInfoDto
        {
            Id = post.Id,
            BlogId = post.BlogId,
            DislikeCount = post.DislikeCount,
            LikeCount = post.LikeCount,
            ViewCount = post.ViewCount,
            PaymentSubscriptionId = post.PaymentSubscriptionId,
            Visibility = post.Visibility,
            Title = post.Title,
            CreatedAt = post.CreatedAt,
            TextInfo = post.Type == PostType.Text ? new TextInfoDto
            {
                Text = post.TextPostInfo.Text,
            } : null,
            VideoInfo = post.Type == PostType.Video ? new VideoInfoDto
            {
                ProcessState = post.ProcessState,
                PreviewUrl = post.VideoPostInfo.PreviewFile == null ? null : await storage.GetFileUrlAsync(post.BlogId, post.VideoPostInfo.PreviewFile!.ObjectName),
                VideoMetadata = post.IsProcessComplete() ? new VideoMetadataModel(
                                post.VideoPostInfo.VideoFile!.Id,
                                post.VideoPostInfo.VideoFile!.Length,
                                post.VideoPostInfo.VideoFile!.Duration,
                                post.VideoPostInfo.VideoFile!.ContentType,
                                post.VideoPostInfo.VideoFile!.ObjectName
                            ) : null
            } : null
        });
    }

    [HttpGet("uploadProgress")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<UploadVideoProgress>> GetPostVideoUploadProgress(Guid fileId)
    {
        return Ok();
    }

    //TODO возможно не нужно, достаточно метода uploadChunk + redis или отдавать проценты при загрузке чанка
    [HttpPost("uploadProgress")]
    [AuthFilter(Roles.Blogger)]
    public async Task<ActionResult<UploadVideoProgress>> CreatePostVideoUploadProgress(CreateUploadVideoProgressRequest request)
    {
        return Ok();
    }


    [HttpGet("availablePostByBlogId/{blogId:guid}")]
    [AuthFilter]
    public async Task<IActionResult> GetAvailablePostPagedByBlogId(Guid blogId, int page, int pageSize, PostType postType = PostType.Video)
    {
        var user = await currentUserService.GetCurrentUserAsync();

        var postQuery = repository.Get<Post>()
            .Where(x => x.IsDelete == false)
            .Where(x => x.BlogId == blogId)
            .Where(x => x.Type == postType);

        if (blogId != user.BlogId)
        {
            postQuery = postQuery.Where(x => x.ProcessState == ProcessState.Complete);
        }

        var totalCount = await postQuery.CountAsync();

        postQuery = postType == PostType.Video
            ? postQuery
                .Include(x => x.VideoPostInfo).ThenInclude(x => x.PostCategories)
                .Include(x => x.VideoPostInfo).ThenInclude(x => x.VideoFile)
                .Include(x => x.VideoPostInfo).ThenInclude(x => x.PreviewFile)
            : postQuery.Include(x => x.TextPostInfo);

        var postList = await postQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        using var storage = fileStorageFactory.CreateFileStorage();

        var result = postList.Select(async x => new PostCommonModelV2
        {
            Id = x.Id,
            BlogId = x.BlogId,
            Title = x.Title,
            PostType = (PostTypeModel)x.Type,
            Text = postType == PostType.Text ?
             x.TextPostInfo.Text
             : null,
            PreviewObjectName = postType == PostType.Video
            ? x.VideoPostInfo.PreviewFile == null ? null : await storage.GetFileUrlAsync(x.BlogId, x.VideoPostInfo.PreviewFile!.ObjectName)
            : null
        });

        return Ok(new PagedListViewModel<PostCommonModelV2>((int)Math.Ceiling((double)totalCount / pageSize), pageSize, await Task.WhenAll(result)));
    }
}
