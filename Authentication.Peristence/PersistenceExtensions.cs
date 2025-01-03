using Authentication.Domain.Entities;
using Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Authentication.Peristence;

public static class PersistenceExtensions
{
    public static void AddAuthenticationPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["ConnectionStrings:AuthenticationDbContext"]!;
        services.AddRepository<AuthenticationDbContext, IAuthEntity>();
        services.AddNpgSqlDbContext<AuthenticationDbContext>(connectionString);
        //services.AddDbContext<AuthenticationDbContext>(option =>
        //option.UseInMemoryDatabase("Auth")    
        ////option.UseNpgsql(connectionString)
        //    );
        //services.AddScoped<IReadWriteRepository<IAuthEntity>, DefaultRepository<AuthenticationDbContext, IAuthEntity>>();

    }
}