using TdpTrans.Repositories;

namespace TdpTrans.Tests
{
    public class ConnectionStringFactoryTests
    {
        [Fact]
        public void Create_WithPostgreSqlUrl_NormalizesToNpgsqlConnectionString()
        {
            var connectionString = SqliteConnectionStringFactory.Create(
                "postgresql://demo-user:demo-pass@ep-demo.neon.tech/tdptrans?sslmode=require&channel_binding=require",
                "C:\\temp");

            Assert.Contains("Host=ep-demo.neon.tech", connectionString);
            Assert.Contains("Port=5432", connectionString);
            Assert.Contains("Database=tdptrans", connectionString);
            Assert.Contains("Username=demo-user", connectionString);
            Assert.Contains("Password=demo-pass", connectionString);
            Assert.Contains("SSL Mode=require", connectionString, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Channel Binding=require", connectionString, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Create_WithoutConfiguredConnectionString_FallsBackToLocalSqliteDatabase()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), $"tdptrans-{Guid.NewGuid():N}");
            var previousDatabaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

            try
            {
                Environment.SetEnvironmentVariable("DATABASE_URL", null);
                var connectionString = SqliteConnectionStringFactory.Create(null, tempDirectory);

                Assert.Contains("tdptrans.db", connectionString, StringComparison.OrdinalIgnoreCase);
                Assert.True(Directory.Exists(Path.Combine(tempDirectory, "App_Data")));
            }
            finally
            {
                Environment.SetEnvironmentVariable("DATABASE_URL", previousDatabaseUrl);
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, recursive: true);
                }
            }
        }
    }
}
