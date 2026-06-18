using Authentication.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;

namespace AuthenticationApplication.HostedServices;

public class TokenCleanerHostedService : BackgroundService
{
    private readonly IServiceProvider serviceProvider;

    public TokenCleanerHostedService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<IAuthEntity>>();

                var now = DateTimeService.Now();
                var expiredTokens = repository.Get<Token>()
                    .Where(x => x.ExpiredAt <= now)
                    .AsAsyncEnumerable();

                await foreach (var token in expiredTokens)
                {
                    repository.Remove(token);
                }

                await repository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}