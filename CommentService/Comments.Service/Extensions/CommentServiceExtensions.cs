using Comments.Domain.Services;
using Comments.Service.Implementation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comments.Service.Extensions
{
    public static class CommentServiceExtensions
    {
        public static void AddCommentService(this IServiceCollection services)
        {
            services.AddScoped<ICommentService, DefaultCommentService>();
        }
    }
}
