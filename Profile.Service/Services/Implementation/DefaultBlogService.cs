using Blog.Domain.Entities;
using Blog.Persistence.Repository.Quries;
using Blog.Service.Exceptions;
using Blog.Service.Models.Blog;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;

namespace Blog.Service.Services.Implementation
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

            var isBlogAlreadyExists = await _context.Get<PersonBlog>()
                .AnyAsync(x => x.UserId == model.UserId);

            if (isBlogAlreadyExists)
            {
                throw new BlogAlreadyExistsException("У данного пользователя уже существует блог");
            }

            var blogId = GuidService.GetNewGuid();

            var blog = new PersonBlog
            {
                Id = blogId,
                CreatedAt = DateTimeOffset.UtcNow,
                Title = model.Title,
                Description = model.Description,
                UserId = model.UserId,
                PhotoUrl = null,//model.PhotoUrl,
            };
            _context.Add(blog);
            await _context.SaveChangesAsync();
            return blog.ToBlogModel();
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
            var blog = await _cacheService.GetCachedDataAsync<PersonBlog>(GetBlogByUserIdKey(userId));
            if (blog == null)
            {
                blog = await _context.Get<PersonBlog>()
                    .Where(x => x.UserId == userId)
                    .FirstOrDefaultAsync() ?? throw new EntityNotFoundException("Не удалось найти блог");

                await _cacheService.SetCachedDataAsync(GetBlogByUserIdKey(userId), blog, TimeSpan.FromMinutes(10));
            }
            return blog.ToBlogModel();
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
    }
}
