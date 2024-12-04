using Company.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;

namespace Company.Persistence
{
    public class CompanyDbContext : BaseDbContext
    {

        public DbSet<Organization> Organizations { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Position> Positions { get; set; }

        public CompanyDbContext(DbContextOptions<CompanyDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            {
                var entity = modelBuilder.Entity<Organization>();
                entity.HasData(new[]
                {
                    new Organization
                    {
                        Id = Guid.Parse("09e91517-37d6-48b6-b43a-78c80ced4f35"),
                        Name = "Тест",
                        DirectorId = Guid.Parse("09f3c24e-6e70-48ea-a5c5-60727af95d1e"),
                        Address = "sda"
                    }
                });
            }
            modelBuilder.Entity<Department>().HasData(new[]
            {
                new Department
                {
                    Id = Guid.Parse("a2a3338a-657c-453b-acc4-0715fd677b59"),
                    OrganizationId =  Guid.Parse("09e91517-37d6-48b6-b43a-78c80ced4f35"),
                    Description = "Разработка чего-то",
                    Name = "Отдел разработки",
                }
            });
            modelBuilder.Entity<Position>().HasData();
        }
    }
}
