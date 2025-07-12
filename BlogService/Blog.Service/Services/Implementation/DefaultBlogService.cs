using Blog.Contracts.Events;
using Blog.Domain.Entities;
using Blog.Service.Exceptions;
using Blog.Service.Models.Blog;
using FileStorage.Service.Service;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;
using System.Text.Json;

namespace Blog.Service.Services.Implementation
{
    internal sealed class DefaultBlogService : IBlogService
    {
        private readonly IReadWriteRepository<IBlogEntity> _context;
        private readonly ICacheService _cacheService;
        private readonly IFileStorageFactory _fileStorageFactory;

        private string GetBlogByIdKey(Guid id) => $"Blog:{id}";
        private string GetBlogByUserIdKey(Guid id) => $"Blog:UserId{id}";

        public DefaultBlogService(IReadWriteRepository<IBlogEntity> context, ICacheService cacheService, IFileStorageFactory fileStorageFactory)
        {
            _context = context;
            _cacheService = cacheService;
            _fileStorageFactory = fileStorageFactory;
        }

        public async Task<BlogModel> CreateBlogAsync(BlogCreateDto model)
        {

            var isBlogAlreadyExists = await _context.Get<PersonBlog>()
                .AnyAsync(x => x.UserId == model.UserId);

            if (isBlogAlreadyExists)
            {
                throw new BlogAlreadyExistsException("У данного пользователя уже существует блог");
            }

            using var storage = _fileStorageFactory.CreateFileStorage();
            var blogId = GuidService.GetNewGuid();

            var photoUrl = model.PhotoUrl == null
                ? null
                : await storage.PutFileAsync(blogId, model.PhotoUrl.FileName, model.PhotoUrl.OpenReadStream());


            var blog = new PersonBlog
            {
                Id = blogId,
                CreatedAt = DateTimeService.Now(),
                Title = model.Title,
                Description = model.Description,
                UserId = model.UserId,
                PhotoUrl = photoUrl,
            };
            _context.Add(blog);
            var @event = new BlogCreateEvent(blogId, blog.UserId);
            _context.Add(new VideoProcessEvent
            {
                EventData = JsonSerializer.Serialize(@event),
                EventType = nameof(BlogCreateEvent),
                CorrelationId = blogId,
            });
            await _context.SaveChangesAsync();
            await _cacheService.RemoveCachedDataAsync(GetBlogByUserIdKey(model.UserId));
            return await blog.ToBlogModel(_fileStorageFactory.CreateFileStorage());
        }

        public async Task DeleteBlogAsync(Guid id)
        {
            var blog = await _context.Get<PersonBlog>()
                .FirstAsync(x => x.Id == id);
            _context.Remove(blog);
            await _context.SaveChangesAsync();
        }

        public async Task<BlogModel> GetBlogByIdAsync(Guid id)
        {
            var result = await _cacheService.GetCachedDataAsync<PersonBlog>(GetBlogByIdKey(id));
            if (result == null)
            {
                result = await _context.Get<PersonBlog>()
                    .FirstAsync(x => x.Id == id);

                await _cacheService.SetCachedDataAsync(GetBlogByIdKey(id), result, TimeSpan.FromMinutes(10));
            }
            return await result.ToBlogModel(_fileStorageFactory.CreateFileStorage());
        }

        public async Task<BlogModel> GetBlogByPostIdAsync(Guid id)
        {
            var blog = await _context.Get<Post>()
                .Where(x => x.Id == id)
                .Select(x => x.Blog)
                .FirstAsync();
            return await blog.ToBlogModel(_fileStorageFactory.CreateFileStorage());
        }

        public async Task<BlogModel> GetBlogByUserIdAsync(Guid userId)
        {
            var blog = await _cacheService.GetCachedDataAsync<PersonBlog>(GetBlogByUserIdKey(userId));
            if (blog == null)
            {
                blog = await _context.Get<PersonBlog>()
                    .Where(x => x.UserId == userId)
                    .FirstOrDefaultAsync() ?? throw new EntityNotFoundException("Не удалось найти блог");

                await _cacheService.SetCachedDataAsync(GetBlogByUserIdKey(userId), blog, TimeSpan.FromMinutes(10));
            }
            return await blog.ToBlogModel(_fileStorageFactory.CreateFileStorage());
        }

        public Task<BlogModel> UpdateBlogAsync(BlogEditDto model)
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

            return blog.ToBlogUserInfoViewModel(hasSubscription);
        }

        public async Task<Guid?> HasUserBlogAsync(Guid userId)
        {
            var isBlogAlreadyExists = await _context.Get<PersonBlog>()
                            .FirstOrDefaultAsync(x => x.UserId == userId);
            return isBlogAlreadyExists?.Id;
        }
    }
}
