using Comments.Domain.Entities;
using Infrastructure.Extensions;
using Infrastructure.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comments.Persistence.Extensions
{
    public static class CommentPersistenceExtensions
    {
        public static void AddCommentPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            //services.AddInMemoryDbContext<ConferenceDbContext>("Conference");
            var connectionString = configuration["ConnectionStrings:CommentDbContext"]!;
            services.AddDefaultRepository<CommentDbContext,ICommentEntity>();
            services.AddNpgSqlDbContext<CommentDbContext>(connectionString);
            services.AddScoped<IDbInitializer, CommentDbInitializer>();
        }
    }
}
