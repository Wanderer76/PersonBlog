using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Persistence;

namespace Authentication.Peristence;

public static class PersistenceExtensions
{
    public static void AddPersistence(this IServiceCollection services)
    {
        services.AddScoped<IReadWriteRepository<AuthenticationDbContext>, DefaultRepository<AuthenticationDbContext>>();
        services.AddDbContext<AuthenticationDbContext>(option=>
            option.UseInMemoryDatabase("Auth"));
    }
}