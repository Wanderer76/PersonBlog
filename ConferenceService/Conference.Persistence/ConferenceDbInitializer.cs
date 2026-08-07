using Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;

namespace Conference.Persistence;

internal sealed class ConferenceDbInitializer(ConferenceDbContext context) : IDbInitializer
{
    public void Initialize()
    {
        context.Database.Migrate();
    }
}
