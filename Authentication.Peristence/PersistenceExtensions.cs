using Authentication.Domain.Entities;
using Infrastructure.Extensions;
using Infrastructure.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Authentication.Peristence;

public static class PersistenceExtensions
{
    public static void AddAuthenticationPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["ConnectionStrings:AuthenticationDbContext"]!;
        services.AddDefaultRepository<AuthenticationDbContext, IAuthEntity>();
        services.AddNpgSqlDbContext<AuthenticationDbContext>(connectionString);
        services.AddScoped<IDbInitializer,AuthDbInitializer>();
        //services.AddDbContext<AuthenticationDbContext>(option =>
        //option.UseInMemoryDatabase("Auth")    
        ////option.UseNpgsql(connectionString)
        //    );
        //services.AddScoped<IReadWriteRepository<IAuthEntity>, DefaultRepository<AuthenticationDbContext, IAuthEntity>>();

    }
}