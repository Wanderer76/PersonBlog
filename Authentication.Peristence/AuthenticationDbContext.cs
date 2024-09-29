using Authentication.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;

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
        modelBuilder.HasDefaultSchema("auth");
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
                    Name = "worker"
                },
                new UserRole
                {
                    Id = Guid.Parse("c2ff298c-dd14-436c-a28b-e2036866ef41"),
                    Name = "manager"
                },

                //new UserRole
                //{
                //    Id = Guid.Parse("c2ff298c-dd14-436c-a28b-e2036866ef42"),
                //    Name = "departmentadmin"
                //},
            });
        }
    }
}