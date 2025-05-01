using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Video.Service.Interface;
using Video.Service.Interface.Default;

namespace Video.Service
{
    public static class VideoServiceExtensions
    {
        public static void AddVideoService(this IServiceCollection service)
        {
            service.AddScoped<IReactionService, DefaultReactionService>();
        }
    }
}
