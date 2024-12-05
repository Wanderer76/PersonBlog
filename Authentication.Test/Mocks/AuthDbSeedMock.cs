using Authentication.Domain.Entities;
using Authentication.Peristence;
using Microsoft.EntityFrameworkCore;
using Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authentication.Test.Mocks
{
    public class AuthDbSeedMock
    {
        public static AuthenticationDbContext Context { get; private set; }
        private readonly List<UserRole> roles = new List<UserRole>
           {
                new UserRole
                {
                    Id = Guid.Parse("57a2b99b-b6ee-4c98-a1f0-b18fe96dae60"),
                    Name = "admin"
                },
                new UserRole
                {
                    Id = Guid.Parse("accbc12f-6ff1-4343-a26f-13b99e64abb6"),
                    Name = "superadmin"
                },
                new UserRole
                {
                    Id = Guid.Parse("d95ca3d6-0f63-4b48-a54f-1202f3d6bf2c"),
                    Name = "worker"
                },
                new UserRole
                {
                    Id = Guid.Parse("c2ff298c-dd14-436c-a28b-e2036866ef41"),
                    Name = "manager"
                },
            };
        private readonly List<AppUser> users = new()
        {
            new AppUser
            {
                Id = Guid.Parse("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
                CreatedAt = DateTimeOffset.UtcNow,
                Login=  "admin1",
                Password = PasswordHasher.GetHash("admin1"),
            }
        };
        private readonly List<AppUserRole> userRoles = new()
        {
             new AppUserRole
             {
                 UserRoleId = Guid.Parse("accbc12f-6ff1-4343-a26f-13b99e64abb6"),
                 AppUserId = Guid.Parse("09f3c24e-6e70-48ea-a5c5-60727af95d1e")
             }
        };
        public AuthDbSeedMock()
        {
            var options = new DbContextOptionsBuilder<AuthenticationDbContext>()
          .UseInMemoryDatabase("AuthenticationDbContext");
            Context = new AuthenticationDbContext(options.Options);


            Context.UserRoles.AddRange(roles);
            Context.AppUsers.AddRange(users);
            Context.AppUserRoles.AddRange(userRoles);

            //Context.AppUsers.Add(new Domain.Entities.AppUser
            //{
            //    Id = Guid.Parse("69a8c779-afd8-4e03-b8bd-7fe4bba30233"),
            //    CreatedAt = DateTime.UtcNow,
            //    Login = "admin",
            //    Password = 
            //});

            Context.SaveChanges();

        }
        //public void Dispose()
        //{
        //    Context.Dispose();
        //}
    }
}
