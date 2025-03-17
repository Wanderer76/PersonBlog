using Conference.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Conference.Service.Extensions
{
    public static class ConferenceServiceExtensions
    {
        public static void AddConferenceService(this IServiceCollection services)
        {
            services.AddScoped<IConferenceRoomService, DefaultConferenceService>();
        }
    }
}
