using Infrastructure.Cache.Services;
using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Profile.Persistence.Repository.Quries;
using Profile.Service.Exceptions;
using Profile.Service.Models.Blog;
using Shared.Persistence;
using Shared.Services;

namespace Profile.Service.Services.Implementation
{
    internal sealed class DefaultBlogService : IBlogService
    {
        private readonly IReadWriteRepository<IProfileEntity> _context;
        private readonly ICacheService _cacheService;

        private string GetBlogByIdKey(Guid id) => $"Blog:{id}";
        private string GetBlogByUserIdKey(Guid id) => $"Blog:UserId{id}";

        public DefaultBlogService(IReadWriteRepository<IProfileEntity> context, ICacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }

        public async Task<BlogModel> CreateBlogAsync(BlogCreateDto model)
        {
            var profile = await _context.GetProfileByUserIdAsync(_cacheService, model.UserId) ?? throw new EntityNotFoundException("Профиль не найден");

            var isBlogAlreadyExists = await _context.Get<Blog>()
                .AnyAsync(x => x.ProfileId == profile.Id);

            if (isBlogAlreadyExists)
            {
                throw new BlogAlreaddyExistsException("У данного пользователя уже существует блог");
            }

            var blogId = GuidService.GetNewGuid();

            var blog = new Blog
            {
                Id = blogId,
                CreatedAt = DateTimeOffset.UtcNow,
                Title = model.Title,
                Description = model.Description,
                ProfileId = profile.Id,
                PhotoUrl = null,//model.PhotoUrl,
                Subscriptions = []
            };
            _context.Add(blog);
            await _context.SaveChangesAsync();
            return blog.ToBlogModel();
        }

        public async Task DeleteBlogAsync(Guid id)
        {
            var blog = await _context.Get<Blog>()
                .FirstAsync(x => x.Id == id);
            _context.Remove(blog);
            await _context.SaveChangesAsync();
        }

        public async Task<BlogModel> GetBlogAsync(Guid id)
        {
            var result = await _cacheService.GetCachedDataAsync<Blog>(GetBlogByIdKey(id));
            if (result == null)
            {
                result = await _context.Get<Blog>()
                    .FirstAsync(x => x.Id == id);

                await _cacheService.SetCachedDataAsync(GetBlogByIdKey(id), result, TimeSpan.FromMinutes(10));
            }
            return result.ToBlogModel();
        }

        public async Task<BlogModel> GetBlogByPostIdAsync(Guid id)
        {
            var blog = await _context.Get<Post>()
                .Where(x => x.Id == id)
                .Select(x => x.Blog)
                .FirstAsync();
            return blog.ToBlogModel();
        }

        public async Task<BlogModel> GetBlogByUserIdAsync(Guid userId)
        {
            var blog = await _cacheService.GetCachedDataAsync<Blog>(GetBlogByUserIdKey(userId));
            if (blog == null)
            {
                var profile = await _context.GetProfileByUserIdAsync(_cacheService, userId) ?? throw new EntityNotFoundException("Профиль не найден");
                blog = await _context.Get<Blog>()
                    .Where(x => x.ProfileId == profile.Id)
                    .FirstOrDefaultAsync() ?? throw new EntityNotFoundException("Не удалось найти блог");

                await _cacheService.SetCachedDataAsync(GetBlogByUserIdKey(userId), blog, TimeSpan.FromMinutes(10));
            }
            return blog.ToBlogModel();
        }

        public Task<BlogModel> UpdateBlogAsync(BlogEditDto model)
        {
            throw new NotImplementedException();
        }
    }
}
