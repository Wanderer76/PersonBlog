using Blog.Contracts.Events;
using Blog.Contracts.Models.Blog;
using Blog.Contracts.Services;
using Blog.Domain.Entities;
using Blog.Service.Models;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;

namespace Blog.Service.Services.Implementation;

internal sealed class DefaultBlogService : IBlogService
{
    private readonly IReadWriteRepository<IBlogEntity> _context;
    private readonly ICacheService _cacheService;
    private readonly IFileStorageFactory _fileStorageFactory;
    private readonly ICurrentUserService _currentUserService;
    public DefaultBlogService(IReadWriteRepository<IBlogEntity> context, ICacheService cacheService, IFileStorageFactory fileStorageFactory, ICurrentUserService currentUserService)
    {
        _context = context;
        _cacheService = cacheService;
        _fileStorageFactory = fileStorageFactory;
        _currentUserService = currentUserService;
    }

    public async Task<Result<BlogModel>> CreateBlogAsync(BlogCreateRequest model)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        var isBlogAlreadyExists = await _context.Get<PersonBlog>()
            .AnyAsync(x => x.UserId == currentUser.UserId);

        if (isBlogAlreadyExists)
        {
            return Result<BlogModel>.Failure(new Error("У данного пользователя уже существует блог"));
        }

        using var storage = _fileStorageFactory.CreateFileStorage();
        var blogId = GuidService.GetNewGuid();

        var photoUrl = model.PhotoUrl == null
            ? null
            : await storage.PutFileAsync(blogId, model.PhotoUrl.FileName, model.PhotoUrl.OpenReadStream());

        var blogResult = PersonBlog.CreateBlog(
            blogId,
            DateTimeService.Now(),
            model.Title,
            model.Description,
            photoUrl,
            currentUser.UserId
        );
        if (blogResult.IsSuccess)
        {
            var blog = blogResult.Value;
            _context.Add(blog);
            var @event = new BlogCreateEvent(blogId, blog.UserId);
            _context.Add(VideoProcessEvent.Create(@event, blogId));
            await _context.SaveChangesAsync();
            await _cacheService.RemoveCachedDataAsync(new BlogByUserIdCacheKey(currentUser.UserId));
            return await blog.ToBlogModel(storage);
        }
        else
        {
            return Result<BlogModel>.Failure(blogResult.Errors!);
        }
    }

    public async Task<Result> DeleteBlogAsync(Guid id)
    {
        var blog = await _context.Get<PersonBlog>()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (blog == null)
        {
            return Result.Failure(nameof(id), "Blog doesn't exists");
        }

        _context.Remove(blog);
        await _context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<BlogModel> GetBlogByIdAsync(Guid id)
    {
        var key = new BlogByIdCacheKey(id);
        var result = await _cacheService.GetOrAddDataAsync(key, () => _context.Get<PersonBlog>().FirstAsync(x => x.Id == id));
        using var storage = _fileStorageFactory.CreateFileStorage();
        return await result.ToBlogModel(storage);
    }

    public async Task<BlogModel> GetBlogByPostIdAsync(Guid id)
    {
        var blog = await _context.Get<Post>()
            .Where(x => x.Id == id)
            .Select(x => x.Blog)
            .FirstAsync();

        using var storage = _fileStorageFactory.CreateFileStorage();
        return await blog.ToBlogModel(storage);
    }

    public async Task<BlogModel> GetBlogByUserIdAsync(Guid userId)
    {
        var key = new BlogByUserIdCacheKey(userId);
        var blog = await _cacheService.GetOrAddDataAsync(key, () => _context.Get<PersonBlog>().Where(x => x.UserId == userId).FirstAsync());
        using var storage = _fileStorageFactory.CreateFileStorage();
        return await blog.ToBlogModel(storage);
    }

    public Task<BlogModel> UpdateBlogAsync(BlogEditRequest model)
    {
        throw new NotImplementedException();
    }

    public async Task<BlogUserInfoViewModel> GetBlogByPostIdAsync(Guid id, Guid? userId)
    {
        var blog = await _context.Get<Post>()
                      .Where(x => x.Id == id)
                      .Select(x => x.Blog)
                      .FirstAsync();

        var hasSubscription = userId.HasValue && await _context.Get<Subscriber>()
            .Where(x => x.BlogId == blog.Id && x.UserId == userId.Value)
            .AnyAsync();

        using var storage = _fileStorageFactory.CreateFileStorage();

        return await blog.ToBlogUserInfoViewModel(hasSubscription, storage);
    }

    public async Task<bool> HasUserBlogAsync(Guid userId)
    {
        var isBlogAlreadyExists = await _context.Get<PersonBlog>()
            .AllAsync(x => x.UserId == userId);
        return isBlogAlreadyExists;
    }
}

public sealed record BlogByUserIdCacheKey(Guid UserId) : ICacheKey
{
    public const string Key = nameof(BlogByUserIdCacheKey);
    public string GetKey() => $"{Key}:{UserId}";
}

public sealed record BlogByIdCacheKey(Guid Id) : ICacheKey
{
    public const string Key = nameof(BlogByIdCacheKey);
    public string GetKey() => $"{Key}:{Id}";
}