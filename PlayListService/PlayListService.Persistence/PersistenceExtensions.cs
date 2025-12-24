using Infrastructure.Extensions;
using Infrastructure.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlayListService.Domain.Entities;

namespace PlayListService.Persistence;

public static class PersistenceExtensions
{
    public static void AddPlayListPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["ConnectionStrings:PlayListDbContext"]!;
        services.AddDefaultRepository<PlayListDbContext, IPlayListEntity>();
        services.AddNpgSqlDbContext<PlayListDbContext>(connectionString);
        services.AddScoped<IDbInitializer,PlayListDbInitializer>();
        //services.AddDbContext<AuthenticationDbContext>(option =>
        //option.UseInMemoryDatabase("Auth")    
        ////option.UseNpgsql(connectionString)
        //    );
        //services.AddScoped<IReadWriteRepository<IAuthEntity>, DefaultRepository<AuthenticationDbContext, IAuthEntity>>();

    }
}