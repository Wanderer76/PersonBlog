using Blog.Contracts.Events;
using Blog.Contracts.Models;
using Blog.Contracts.Models.Post;
using Blog.Contracts.Services;
using Blog.Domain.Entities;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;

namespace Blog.Service.Services.Implementation;

internal sealed class DefaultProfilePostV2Service(
    IReadWriteRepository<IBlogEntity> repository,
    IFileStorageFactory fileStorageFactory,
    ICurrentUserService currentUserService,
    ISubscriptionLevelService subscriptionLevelService,
    ICategoryService categoryService)
    : IProfilePostV2Service
{
    public async Task<PagedListViewModel<UserPostInfoModel>> GetCurrentUserPostsAsync(
            Guid blogId, int page, int pageSize, PostType postType)
    {
        var query = BuildBasePostQuery(blogId, postType);
        var totalCount = await query.CountAsync();
        var posts = await ApplyIncludes(query, postType)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = await MapToUserPostInfoDtosAsync(posts, blogId, postType);
        return new PagedListViewModel<UserPostInfoModel>(
            (int)Math.Ceiling((double)totalCount / pageSize),
            pageSize,
            dtos);
    }

    public async Task<CreatePostModelViewModel> GetPostCreateModelAsync()
    {
        var visibility = Enum.GetValues<PostVisibility>().Select(x => new SelectItem<PostVisibility>(x, x.FormatName()));
        var subscriptions = await subscriptionLevelService.GetAllSubscriptionsAsync();
        var categories = await categoryService.GetAllCategoriesAsync();
        return new CreatePostModelViewModel(subscriptions, visibility, categories);
    }

    public async Task<Result<UserPostInfoModel>> CreatePostAsync(PostCreateCommand command)
    {
        var user = await currentUserService.GetCurrentUserAsync();
        var blogId = user.BlogId;
        var postId = GuidService.GetNewGuid();
        var categories = command.CategoryIds != null && command.CategoryIds.Any()
            ? await categoryService.GetCategoriesByIdsAsync(command.CategoryIds)
            : [];

        var post = new Post(
            id: postId,
            blogId: blogId,
            type: command.Type,
            description: command.Description,
            title: command.Title,
            paymentSubscriptionId: null,
            visibility: command.Visibility,
            categories: categories.Select(c => c.Id).ToList(),
            text: command.TextContent?.Trim());

        using var storage = fileStorageFactory.CreateFileStorage();
        await ProcessPostFilesAsync(post, command, blogId, postId, storage);

        repository.Add(post);
        await repository.SaveChangesAsync();

        return await MapToUserPostInfoDtoAsync(post, blogId, storage);
    }

    public async Task<PagedListViewModel<PostCommonModelV2>> GetAvailablePostsByBlogIdAsync(Guid requestedBlogId, int page, int pageSize, PostType postType)
    {
        var query = BuildBasePostQuery(requestedBlogId, postType);

        var user = await currentUserService.GetCurrentUserAsync();
        var currentBlogId = user.BlogId;

        if (requestedBlogId != currentBlogId)
        {
            query = query.Where(x => x.ProcessState == ProcessState.Complete);
        }

        var totalCount = await query.CountAsync();
        var posts = await ApplyIncludes(query, postType)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = await MapToPostCommonModelDtosAsync(posts, requestedBlogId, postType);
        return new PagedListViewModel<PostCommonModelV2>(
            (int)Math.Ceiling((double)totalCount / pageSize),
            pageSize,
            dtos);
    }

    private IQueryable<Post> BuildBasePostQuery(Guid blogId, PostType postType) =>
        repository.Get<Post>()
            .Where(x => !x.IsDelete && x.BlogId == blogId && x.Type == postType);

    private IQueryable<Post> ApplyIncludes(IQueryable<Post> query, PostType postType) =>
        postType == PostType.Video
            ? query
                .Include(x => x.VideoPostInfo).ThenInclude(x => x.PostCategories)
                .Include(x => x.VideoPostInfo).ThenInclude(x => x.VideoFile)
                .Include(x => x.VideoPostInfo).ThenInclude(x => x.PreviewFile)
            : query.Include(x => x.TextPostInfo);

    private async Task<List<UserPostInfoModel>> MapToUserPostInfoDtosAsync(
        List<Post> posts, Guid blogId, PostType postType)
    {
        using var storage = fileStorageFactory.CreateFileStorage();
        var tasks = posts.Select(post => MapToUserPostInfoDtoAsync(post, blogId, storage));
        return [.. (await Task.WhenAll(tasks))];
    }

    private async Task<UserPostInfoModel> MapToUserPostInfoDtoAsync(
        Post post, Guid blogId, IFileStorage storage)
    {
        return new UserPostInfoModel
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
            TextInfo = post.Type == PostType.Text
                ? new TextInfoDto { Text = post.TextPostInfo.Text }
                : null,
            VideoInfo = post.Type == PostType.Video
                ? new VideoInfoDto
                {
                    ProcessState = post.ProcessState,
                    PreviewUrl = post.VideoPostInfo.PreviewFile == null
                        ? null
                        : await storage.GetFileUrlAsync(blogId, post.VideoPostInfo.PreviewFile.ObjectName),
                    VideoMetadata = post.IsProcessComplete()
                        ? new VideoMetadataModel(
                            post.VideoPostInfo.VideoFile!.Id,
                            post.VideoPostInfo.VideoFile!.Length,
                            post.VideoPostInfo.VideoFile!.Duration,
                            post.VideoPostInfo.VideoFile!.ContentType,
                            post.VideoPostInfo.VideoFile!.ObjectName)
                        : null
                }
                : null
        };
    }

    private async Task<List<PostCommonModelV2>> MapToPostCommonModelDtosAsync(
        List<Post> posts, Guid blogId, PostType postType)
    {
        using var storage = fileStorageFactory.CreateFileStorage();
        var tasks = posts.Select(async post => new PostCommonModelV2
        {
            Id = post.Id,
            BlogId = post.BlogId,
            Title = post.Title,
            PostType = (PostTypeModel)post.Type,
            Text = postType == PostType.Text ? post.TextPostInfo.Text : null,
            PreviewUrl = postType == PostType.Video && post.VideoPostInfo.PreviewFile != null
                ? await storage.GetFileUrlAsync(blogId, post.VideoPostInfo.PreviewFile.ObjectName)
                : null
        });
        return (await Task.WhenAll(tasks)).ToList();
    }


    private async Task ProcessPostFilesAsync(
        Post post,
        PostCreateCommand command,
        Guid blogId,
        Guid postId,
        IFileStorage storage)
    {
        if (command.Type == PostType.Text && command.TextFiles?.Any() == true)
        {
            var files = new List<PostFile>();
            foreach (var file in command.TextFiles)
            {
                if (file.Length == 0) continue;

                var fileId = GuidService.GetNewGuid();
                var objectName = await storage.PutFileAsync(
                    blogId,
                    $"{postId}/{fileId}",
                    file.ContentStream);

                files.Add(new PostFile
                {
                    Id = fileId,
                    PostId = postId,
                    ObjectName = objectName,
                    Name = Path.GetFileName(file.FileName),
                    ContentType = file.ContentType,
                    Length = file.Length,
                    CreatedAt = DateTimeService.Now(),
                    FileExtension = Path.GetExtension(file.FileName)
                });
            }
            post.TextPostInfo = new TextPostInfo(postId, command.TextContent ?? string.Empty, files);
        }

        if (command.Type == PostType.Video && command.Thumbnail != null && command.Thumbnail.Length > 0)
        {
            var thumbId = GuidService.GetNewGuid();
            var objectName = await storage.PutFileAsync(
                blogId,
                $"{postId}/{thumbId}",
                command.Thumbnail.ContentStream);

            var previewFile = new PostFile
            {
                Id = thumbId,
                PostId = postId,
                ObjectName = objectName,
                Name = Path.GetFileName(command.Thumbnail.FileName),
                FileExtension = Path.GetExtension(command.Thumbnail.FileName),
                ContentType = command.Thumbnail.ContentType,
                Length = command.Thumbnail.Length,
                CreatedAt = DateTimeService.Now()
            };

            post.VideoPostInfo.PreviewFile = previewFile;
            post.VideoPostInfo.PreviewId = previewFile.Id;
        }
    }

    public async Task<Result> RemovePostAsync(Guid postId)
    {
        var user = await currentUserService.GetCurrentUserAsync();

        var post = await repository.Get<Post>()
            .Include(x => x.VideoPostInfo)
            .FirstOrDefaultAsync(x => x.Id == postId);

        if (post == null)
            return Result.Failure(new Error("postId", "Not found"));

        if (post.BlogId != user.BlogId)
        {
            return Result.Failure(new Error("postId", "Пост не принадлежит пользователю"));
        }
        repository.Attach(post);
        post.Delete();
        repository.Add(new PostRemoveEvent(post.Id, DateTimeService.Now()));
        repository.Add(VideoProcessEvent.Create(new PostUpdateEvent
        {
            BlogId = post.BlogId,
            CreatedAt = DateTimeService.Now(),
            UpdateType = UpdateType.Delete,
            Description = post.VideoPostInfo.Description,
            PostId = post.Id,
            Title = post.Title,
            ViewCount = post.ViewCount
        }));
        await repository.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<PostEditViewModel>> GetPostEditViewModelAsync(Guid postId)
    {
        var user = await currentUserService.GetCurrentUserAsync();

        var post = await repository.Get<Post>()
            .Include(x => x.VideoPostInfo)
            .ThenInclude(x => x.PreviewFile)
            .Include(x => x.VideoPostInfo.PostCategories)
            .FirstOrDefaultAsync(x => x.Id == postId);

        if (post == null)
            return Result<PostEditViewModel>.Failure(new Error("postId", "Not found"));

        if (post.BlogId != user.BlogId)
        {
            return Result<PostEditViewModel>.Failure(new Error("postId", "Пост не принадлежит пользователю"));
        }
        using var storage = fileStorageFactory.CreateFileStorage();

        return new PostEditViewModel(
            id: post.Id,
            title: post.Title,
            description: post.VideoPostInfo.Description,
            previewUrl: await storage.GetFileUrlAsync(post.BlogId, post.VideoPostInfo!.PreviewFile!.ObjectName),
            visibility: post.Visibility,
            paymentSubscriptionId: post.PaymentSubscriptionId,
            categories: post.VideoPostInfo.PostCategories.Select(x => x.CategoryId).ToList()
            );

    }

    public async Task<Result> UpdatePostAsync(PostUpdateRequest updateRequest)
    {
        var user = await currentUserService.GetCurrentUserAsync();

        var post = await repository.Get<Post>()
            .Include(x => x.VideoPostInfo)
            .Include(x => x.VideoPostInfo.PreviewFile)
            .Include(x => x.VideoPostInfo.PostCategories)
            .FirstOrDefaultAsync(x => x.Id == updateRequest.Id);

        if (post == null)
            return Result.Failure(new Error("id", "Not found"));

        if (post.BlogId != user.BlogId)
        {
            return Result.Failure(new Error("id", "Пост не принадлежит пользователю"));
        }

        using var storage = fileStorageFactory.CreateFileStorage();

        repository.Attach(post);

        post.VideoPostInfo.Description = updateRequest.Description;
        post.Title = updateRequest.Title;

        var categoriesToRemove = updateRequest.Categories
            .Except(post.VideoPostInfo.PostCategories.Select(x => x.CategoryId))
            .ToList();

        foreach (var category in post.VideoPostInfo.PostCategories.Where(x => categoriesToRemove.Contains(x.CategoryId)))
        {
            repository.Remove(category);
        }

        var newCategories = await repository.Get<Category>()
            .Where(x => updateRequest.Categories.Contains(x.Id))
            .ToListAsync();

        foreach (var i in newCategories)
        {
            post.AddCategory(i);
        }

        if (updateRequest.Preview != null)
        {
            var preview = post.VideoPostInfo.PreviewFile!;
            repository.Remove(preview);
            var fileId = GuidService.GetNewGuid();
            var newThumbnail = new PostFile
            {
                Id = fileId,
                ContentType = updateRequest.Preview.ContentType,
                CreatedAt = DateTimeService.Now(),
                FileExtension = updateRequest.Preview.FileExtension,
                Length = updateRequest.Preview.Length,
                Name = updateRequest.Preview.Name,
                ObjectName = $"{post.Id}/{fileId}",
                PostId = post.Id,
            };
            repository.Add(newThumbnail);

            await storage.PutFileAsync(post.BlogId, newThumbnail.ObjectName, updateRequest.Preview.ContentStream);
            post.VideoPostInfo.PreviewId = newThumbnail.Id;
            await storage.RemoveFileAsync(post.BlogId, preview.ObjectName);
        }

        repository.Add(VideoProcessEvent.Create(new PostUpdateEvent
        {
            BlogId = post.BlogId,
            CreatedAt = DateTimeService.Now(),
            UpdateType = UpdateType.Update,
            Description = post.VideoPostInfo.Description,
            PostId = post.Id,
            Title = post.Title,
            ViewCount = post.ViewCount
        }));

        await repository.SaveChangesAsync();

        return Result.Success();
    }
}
