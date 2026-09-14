using Blog.Domain.Entities;
using Infrastructure.Extensions;
using Infrastructure.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Blog.Persistence
{
    public static class BlogPersistenceExtensions
    {
        public static void AddBlogPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration["ConnectionStrings:BlogDbContext"]!;
            services.AddNpgSqlDbContext<BlogDbContext>(connectionString);
            services.AddScoped<IDbInitializer, BlogDbInitializer>();
            services.AddDefaultRepository<BlogDbContext, IBlogEntity>();
        }
    }
}
