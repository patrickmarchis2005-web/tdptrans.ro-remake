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

            optionsBuilder.UseSqlite(
                SqliteConnectionStringFactory.Create(null, controllersProjectPath),
                sqliteOptions => sqliteOptions.MigrationsAssembly("TdpTrans.Repositories"));

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
