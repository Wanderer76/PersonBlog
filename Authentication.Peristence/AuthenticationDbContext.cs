using Authentication.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;

namespace Authentication.Peristence;

public class AuthenticationDbContext : BaseDbContext
{
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<AppUserRole> AppUserRoles { get; set; }
    public DbSet<Token> Tokens { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
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
                    Name = "user"
                },
                new UserRole
                {
                    Id = Guid.Parse("c2ff298c-dd14-436c-a28b-e2036866ef41"),
                    Name = "bloger"
                },

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
            entity.HasData(new[]
            {
                 new AppUserRole
                 {
                     UserRoleId = Guid.Parse("accbc12f-6ff1-4343-a26f-13b99e64abb6"),
                     AppUserId = Guid.Parse("09f3c24e-6e70-48ea-a5c5-60727af95d1e")
                 }
            });
        }
    }
}