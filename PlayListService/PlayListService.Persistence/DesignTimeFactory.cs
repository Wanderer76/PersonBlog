using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PlayListService.Persistence;

public class DesignTimeFactory : IDesignTimeDbContextFactory<PlayListDbContext>
{
    public PlayListDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PlayListDbContext>();
        optionsBuilder.UseNpgsql("Server=localhost;Port=5432;SearchPath=PlayList;Database=person_blog;User Id=postgres;Password=1;");
        return new PlayListDbContext(optionsBuilder.Options);
    }
}
