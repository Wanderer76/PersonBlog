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

        public DefaultBlogService(IReadWriteRepository<IProfileEntity> context)
        {
            _context = context;
        }

        public async Task<BlogModel> CreateBlogAsync(BlogCreateDto model)
        {
            var profile = await _context.GetProfileByIdAsync(model.ProfileId) ?? throw new ProfileNotFoundException("Профиль не найден");

            var isBlogAlreadyExists = await _context.Get<Blog>().AnyAsync(x => x.ProfileId == profile.Id);

            if (isBlogAlreadyExists)
            {
                throw new BlogAlreaddyExistsException("У данного пользователя уже существует блог");
            }
            var blogId = GuidService.GetNewGuid();

            var blog = new Blog
            {
                Id = blogId,
                CreatedAt = DateTimeOffset.UtcNow,
                Name = model.Title,
                Description = model.Description,
                ProfileId = profile.Id,
                PhotoUrl = model.PhotoUrl,
            };
            _context.Add(blog);
            await _context.SaveChangesAsync();
            return blog.ToBlogModel();
        }

        public Task DeleteBlogAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<BlogModel> GetBlogAsync(Guid id)
        {
            var result = await _context.Get<Blog>()
                .FirstAsync(x => x.Id == id);
            return result.ToBlogModel();
        }

        public Task<BlogModel> UpdateBlogAsync(BlogEditDto model)
        {
            throw new NotImplementedException();
        }
    }
}
