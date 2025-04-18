using Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;

namespace VideoReacting.Persistence
{
    internal class ViewReactingDbInitializer : IDbInitializer
    {
        private readonly ViewReactingDbContext _dbContext;

        public ViewReactingDbInitializer(ViewReactingDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void Initialize()
        {
            _dbContext.Database.Migrate();
        }
    }
}
