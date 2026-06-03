using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TdpTrans.Repositories
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            var controllersProjectPath = SqliteConnectionStringFactory.ResolveControllersContentRoot(Directory.GetCurrentDirectory());
            var configuredConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? Environment.GetEnvironmentVariable("DATABASE_URL");

            SqliteConnectionStringFactory.Configure(optionsBuilder, configuredConnectionString, controllersProjectPath);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
