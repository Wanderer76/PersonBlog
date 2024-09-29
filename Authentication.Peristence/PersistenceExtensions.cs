using Authentication.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Persistence;

namespace Authentication.Peristence;

public static class PersistenceExtensions
{
    public static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["ConnectionStrings:AuthenticationContext"];
        services.AddDbContext<AuthenticationDbContext>(option =>
            option.UseNpgsql(connectionString));
        services.AddScoped<IReadWriteRepository<IAuthEntity>, DefaultRepository<AuthenticationDbContext, IAuthEntity>>();

    }
}