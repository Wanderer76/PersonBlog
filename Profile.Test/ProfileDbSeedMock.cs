using Microsoft.EntityFrameworkCore;
using Profile.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profile.Test.Mocks
{
    public class ProfileDbSeedMock
    {
        public  ProfileDbContext Context { get; private set; }
        public  IEnumerable<Guid> UserIds { get; private set; } = new HashSet<Guid>()
        {
            Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),
        };
        public ProfileDbSeedMock()
        {
            var options = new DbContextOptionsBuilder<ProfileDbContext>()
          .UseInMemoryDatabase("ProfileDbContext");
            Context = new ProfileDbContext(options.Options);

            Context.SaveChanges();

        }
        //public void Dispose()
        //{
        //    Context.Dispose();
        //}
    }
}
