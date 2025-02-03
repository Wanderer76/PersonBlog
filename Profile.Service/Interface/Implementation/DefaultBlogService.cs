using MessageBus;
using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Profile.Persistence.Repository;
using Profile.Service.Exceptions;
using Profile.Service.Models.Blog;
using Shared.Persistence;
using Shared.Services;

namespace Profile.Service.Interface.Implementation
{
    internal class DefaultBlogService : IBlogService
    {
        private readonly IReadWriteRepository<IProfileEntity> _context;
        private readonly IMessageBus _messageBus;

        public DefaultBlogService(IReadWriteRepository<IProfileEntity> context, IMessageBus messageBus)
        {
            _context = context;
            _messageBus = messageBus;
        }

        public async Task<BlogModel> CreateBlogAsync(BlogCreateDto model)
        {
            var profile = await _context.GetProfileByUserIdAsync(model.UserId) ?? throw new EntityNotFoundException("Профиль не найден");

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
                PhotoUrl = null//model.PhotoUrl,
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
            var result = await _context.Get<Blog>()
                .FirstAsync(x => x.Id == id);
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
            //await _messageBus.SendMessageAsync("quueue", new { Name = "asdds", Number = 12321 }, (entity) =>
            //{
            //    Console.WriteLine(entity);
            //});
            var profile = await _context.GetProfileByUserIdAsync(userId) ?? throw new EntityNotFoundException("Профиль не найден");
            var blog = await _context.Get<Blog>()
                .Where(x => x.ProfileId == profile.Id)
                .FirstOrDefaultAsync() ?? throw new EntityNotFoundException("Не удалось найти блог");

            return blog.ToBlogModel();
        }

        public Task<BlogModel> UpdateBlogAsync(BlogEditDto model)
        {
            throw new NotImplementedException();
        }
    }
}
