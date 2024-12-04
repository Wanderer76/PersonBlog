using Microsoft.Extensions.DependencyInjection;
using Profile.Service.Interface;
using Profile.Service.Interface.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profile.Service.Extensions
{
    public static class ProfileServiceExtensions
    {
        public static void AddProfileServices(this IServiceCollection services)
        {
            services.AddScoped<IProfileService, DefaultProfileService>();
        }
    }
}
