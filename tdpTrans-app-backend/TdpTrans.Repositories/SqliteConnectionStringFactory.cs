using Microsoft.Data.Sqlite;

namespace TdpTrans.Repositories
{
    public static class SqliteConnectionStringFactory
    {
        private const string DefaultConnectionString = "Data Source=App_Data/tdptrans.db";

        public static string Create(string? configuredConnectionString, string contentRootPath)
        {
            var connectionString = string.IsNullOrWhiteSpace(configuredConnectionString)
                ? DefaultConnectionString
                : configuredConnectionString;

            var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);

            if (string.IsNullOrWhiteSpace(connectionStringBuilder.DataSource))
            {
                connectionStringBuilder.DataSource = Path.Combine(contentRootPath, "App_Data", "tdptrans.db");
            }
            else if (!Path.IsPathRooted(connectionStringBuilder.DataSource))
            {
                connectionStringBuilder.DataSource = Path.GetFullPath(Path.Combine(contentRootPath, connectionStringBuilder.DataSource));
            }

            var databaseDirectory = Path.GetDirectoryName(connectionStringBuilder.DataSource);
            if (!string.IsNullOrWhiteSpace(databaseDirectory))
            {
                Directory.CreateDirectory(databaseDirectory);
            }

            return connectionStringBuilder.ToString();
        }

        public static string ResolveControllersContentRoot(string startPath)
        {
            var currentDirectory = new DirectoryInfo(Path.GetFullPath(startPath));

            while (currentDirectory is not null)
            {
                if (File.Exists(Path.Combine(currentDirectory.FullName, "TdpTrans.Controllers.csproj")))
                {
                    return currentDirectory.FullName;
                }

                var childProjectDirectory = Path.Combine(currentDirectory.FullName, "TdpTrans.Controllers");
                if (File.Exists(Path.Combine(childProjectDirectory, "TdpTrans.Controllers.csproj")))
                {
                    return childProjectDirectory;
                }

                currentDirectory = currentDirectory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the TdpTrans.Controllers project directory.");
        }
    }
}
