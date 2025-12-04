using Authentication.Contract.Constants;
using Authentication.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;

namespace Authentication.Peristence;

public class AuthenticationDbContext : BaseDbContext
{
    public DbSet<AppProfile> Profiles { get; set; }
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<AppUserRole> AppUserRoles { get; set; }
    public DbSet<Token> Tokens { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<AuthEvent> AuthEvents { get; set; }
    public AuthenticationDbContext(DbContextOptions<AuthenticationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Authentication");
        base.OnModelCreating(modelBuilder);
        {
            var entity = modelBuilder.Entity<UserRole>();
            entity.HasData(new[]
           {
                new UserRole
                {
                    Id = Roles.AdminRoleId,
                    Name = "admin"
                },
                new UserRole
                {
                    Id = Roles.SuperAdminRoleId,
                    Name = "superadmin"
                },
                new UserRole
                {
                    Id = Roles.UserRoleId,
                    Name = "user"
                },
                new UserRole
                {
                    Id = Roles.BloggerRoleId,
                    Name = "blogger"
                },
                new UserRole
                {
                    Id = Roles.ArtistRoleId,
                    Name = "artist"
                }
                //new UserRole
                //{
                //    Id = Guid.Parse("c2ff298c-dd14-436c-a28b-e2036866ef42"),
                //    Name = "departmentadmin"
                //},
            });
        }
        {
            var entity = modelBuilder.Entity<AppUser>();
            entity.HasData(new[]
            {
                new AppUser
                {
                    Id = Guid.Parse("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
                    CreatedAt = DateTimeOffset.UtcNow,
                    Login = "admin",
                    Password = PasswordHasher.GetHash("admin"),

                }
            });
        }
        {
            var entity = modelBuilder.Entity<AppUserRole>();
            entity.HasKey(x => new { x.AppUserId, x.UserRoleId });
            entity.HasData(new[]
            {
                 new AppUserRole
                 {
                     UserRoleId = Guid.Parse("accbc12f-6ff1-4343-a26f-13b99e64abb6"),
                     AppUserId = Guid.Parse("09f3c24e-6e70-48ea-a5c5-60727af95d1e")
                 }
            });
        }
        {
            var entity = modelBuilder.Entity<AppProfile>();
            entity.HasIndex(x => new { x.UserId, x.IsDeleted }).IsUnique();

            //entity.HasData(new[]
            //{
            //        AppProfile.Create(
            //            userId: Guid.Parse("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
            //            email :"ateplinsky@mail.ru",
            //            name :"Артём")

            //    });
        }
    }
}