using Company.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Persistence;

namespace Company.Persistence
{
    public static class CompayPersistenceExtension
    {
        public static void AddCompanyPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<CompanyDbContext>(options =>
            {
                options.UseInMemoryDatabase("Company");
            });
            services.AddScoped<IReadWriteRepository<ICompanyEntity>, DefaultRepository<CompanyDbContext, ICompanyEntity>>();
        }
    }
}
