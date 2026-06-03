using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace TdpTrans.Repositories
{
    public static class SqliteConnectionStringFactory
    {
        private const string DefaultConnectionString = "Data Source=App_Data/tdptrans.db";

        public static string Create(string? configuredConnectionString, string contentRootPath)
        {
            var connectionString = string.IsNullOrWhiteSpace(configuredConnectionString)
                ? Environment.GetEnvironmentVariable("DATABASE_URL") ?? DefaultConnectionString
                : configuredConnectionString.Trim();

            if (IsPostgreSqlConnectionString(connectionString))
            {
                return NormalizePostgreSqlConnectionString(connectionString);
            }

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

        public static void Configure(DbContextOptionsBuilder optionsBuilder, string? configuredConnectionString, string contentRootPath)
        {
            var resolvedConnectionString = Create(configuredConnectionString, contentRootPath);

            if (IsPostgreSqlConnectionString(resolvedConnectionString))
            {
                optionsBuilder.UseNpgsql(
                    resolvedConnectionString,
                    npgsqlOptions => npgsqlOptions.MigrationsAssembly("TdpTrans.Repositories"));
                return;
            }

            optionsBuilder.UseSqlite(
                resolvedConnectionString,
                sqliteOptions => sqliteOptions.MigrationsAssembly("TdpTrans.Repositories"));
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

        private static bool IsPostgreSqlConnectionString(string connectionString)
        {
            var normalizedConnectionString = connectionString.Trim();

            return normalizedConnectionString.StartsWith("Host=", StringComparison.OrdinalIgnoreCase)
                || normalizedConnectionString.StartsWith("Username=", StringComparison.OrdinalIgnoreCase)
                || normalizedConnectionString.StartsWith("User ID=", StringComparison.OrdinalIgnoreCase)
                || normalizedConnectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
                || normalizedConnectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePostgreSqlConnectionString(string connectionString)
        {
            if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
                && !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                return connectionString;
            }

            var uri = new Uri(connectionString);
            var builder = new StringBuilder();

            AppendConnectionStringPart(builder, "Host", uri.Host);
            AppendConnectionStringPart(builder, "Port", uri.IsDefaultPort ? "5432" : uri.Port.ToString());
            AppendConnectionStringPart(builder, "Database", uri.AbsolutePath.Trim('/'));

            if (!string.IsNullOrWhiteSpace(uri.UserInfo))
            {
                var userInfoParts = uri.UserInfo.Split(':', 2);
                AppendConnectionStringPart(builder, "Username", Uri.UnescapeDataString(userInfoParts[0]));

                if (userInfoParts.Length > 1)
                {
                    AppendConnectionStringPart(builder, "Password", Uri.UnescapeDataString(userInfoParts[1]));
                }
            }

            var queryParameters = ParseQueryString(uri.Query);
            AppendConnectionStringPart(builder, "SSL Mode", GetQueryParameter(queryParameters, "sslmode") ?? "Require");

            var channelBinding = GetQueryParameter(queryParameters, "channel_binding");
            if (!string.IsNullOrWhiteSpace(channelBinding))
            {
                AppendConnectionStringPart(builder, "Channel Binding", channelBinding);
            }

            var trustServerCertificate = GetQueryParameter(queryParameters, "trust_server_certificate");
            if (!string.IsNullOrWhiteSpace(trustServerCertificate))
            {
                AppendConnectionStringPart(builder, "Trust Server Certificate", trustServerCertificate);
            }

            return builder.ToString();
        }

        private static Dictionary<string, string> ParseQueryString(string queryString)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(queryString))
            {
                return values;
            }

            foreach (var part in queryString.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var keyValuePair = part.Split('=', 2);
                var key = Uri.UnescapeDataString(keyValuePair[0]);
                var value = keyValuePair.Length > 1
                    ? Uri.UnescapeDataString(keyValuePair[1])
                    : string.Empty;

                values[key] = value;
            }

            return values;
        }

        private static string? GetQueryParameter(IReadOnlyDictionary<string, string> queryParameters, string key)
        {
            return queryParameters.TryGetValue(key, out var value)
                ? value
                : null;
        }

        private static void AppendConnectionStringPart(StringBuilder builder, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(';');
            }

            builder.Append(key);
            builder.Append('=');
            builder.Append(value);
        }
    }
}
