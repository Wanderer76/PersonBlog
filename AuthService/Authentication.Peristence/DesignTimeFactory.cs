//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Design;

//namespace Authentication.Peristence
//{
//    public class DesignTimeFactory : IDesignTimeDbContextFactory<AuthenticationDbContext>
//    {
//        public AuthenticationDbContext CreateDbContext(string[] args)
//        {
//            var optionsBuilder = new DbContextOptionsBuilder<AuthenticationDbContext>();
//            optionsBuilder.UseNpgsql("Server=localhost;Port=5432;SearchPath=auth;Database=PersonManagement;User Id=postgres;Password=1;");
//            return new AuthenticationDbContext(optionsBuilder.Options);
//        }
//    }
//}
