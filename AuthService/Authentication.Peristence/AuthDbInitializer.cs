using Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;

namespace Authentication.Peristence
{
    internal class AuthDbInitializer : IDbInitializer
    {
        private readonly AuthenticationDbContext _dbContext;

        public AuthDbInitializer(AuthenticationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void Initialize()
        {
            _dbContext.Database.Migrate();
        }
    }
}
