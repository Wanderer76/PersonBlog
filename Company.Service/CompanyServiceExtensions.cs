using Company.Service.Service;
using Company.Service.Service.Impl;
using Microsoft.Extensions.DependencyInjection;

namespace Company.Service
{
    public static class CompanyServiceExtensions
    {
        public static void AddCompanyService(this IServiceCollection services)
        {
            services.AddScoped<ICompanyService, DefaultCompanyService>();
        }
    }
}
