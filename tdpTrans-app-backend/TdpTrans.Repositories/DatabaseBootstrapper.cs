using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using TdpTrans.Models;

namespace TdpTrans.Repositories
{
    public static class DatabaseBootstrapper
    {
        private const string DefaultAdminAuthenticationPhrase = "TDP-ADMIN";
        private const string DefaultUserAuthenticationPhrase = "TDP-USER";

        public static async Task UpgradeAsync(ApplicationDbContext dbContext)
        {
            if (!dbContext.Database.IsSqlite())
            {
                return;
            }

            var connection = dbContext.Database.GetDbConnection();
            var shouldCloseConnection = connection.State != System.Data.ConnectionState.Open;

            if (shouldCloseConnection)
            {
                await connection.OpenAsync();
            }

            try
            {
                await EnsureColumn(connection, "Users", "SecurityCodeHash", "TEXT NOT NULL DEFAULT ''");
                await EnsureColumn(connection, "Users", "AuthenticationPhraseHash", "TEXT NOT NULL DEFAULT ''");

                await ExecuteNonQuery(
                    connection,
                    """
                    CREATE TABLE IF NOT EXISTS "AuthSessions" (
                        "Id" INTEGER NOT NULL CONSTRAINT "PK_AuthSessions" PRIMARY KEY AUTOINCREMENT,
                        "UserId" INTEGER NOT NULL,
                        "TokenHash" TEXT NOT NULL,
                        "CreatedAtUtc" TEXT NOT NULL,
                        "LastActivityAtUtc" TEXT NOT NULL,
                        "ExpiresAtUtc" TEXT NOT NULL,
                        "RevokedAtUtc" TEXT NULL,
                        "ClientKey" TEXT NOT NULL DEFAULT '',
                        "UserAgent" TEXT NOT NULL DEFAULT '',
                        "RemoteIpAddress" TEXT NOT NULL DEFAULT '',
                        CONSTRAINT "FK_AuthSessions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
                    );
                    """);

                await ExecuteNonQuery(connection, "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_AuthSessions_TokenHash\" ON \"AuthSessions\" (\"TokenHash\");");
                await ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS \"IX_AuthSessions_UserId\" ON \"AuthSessions\" (\"UserId\");");
                await ExecuteNonQuery(
                    connection,
                    """
                    CREATE TABLE IF NOT EXISTS "UserObservations" (
                        "Id" INTEGER NOT NULL CONSTRAINT "PK_UserObservations" PRIMARY KEY AUTOINCREMENT,
                        "UserId" INTEGER NOT NULL,
                        "Reason" TEXT NOT NULL,
                        "Details" TEXT NOT NULL,
                        "RiskScore" INTEGER NOT NULL,
                        "IsActive" INTEGER NOT NULL DEFAULT 1,
                        "FirstDetectedAtUtc" TEXT NOT NULL,
                        "LastDetectedAtUtc" TEXT NOT NULL,
                        CONSTRAINT "FK_UserObservations_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
                    );
                    """);

                await ExecuteNonQuery(
                    connection,
                    """
                    CREATE TABLE IF NOT EXISTS "ChatMessages" (
                        "Id" TEXT NOT NULL CONSTRAINT "PK_ChatMessages" PRIMARY KEY,
                        "ConversationKey" TEXT NOT NULL,
                        "SenderUserId" INTEGER NOT NULL,
                        "SenderName" TEXT NOT NULL,
                        "SenderRole" TEXT NOT NULL,
                        "RecipientUserId" INTEGER NOT NULL,
                        "RecipientName" TEXT NOT NULL,
                        "RecipientRole" TEXT NOT NULL,
                        "Message" TEXT NOT NULL,
                        "TimestampUtc" TEXT NOT NULL
                    );
                    """);

                await ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS \"IX_ActivityLogs_UserId_ActionType_TimestampUtc\" ON \"ActivityLogs\" (\"UserId\", \"ActionType\", \"TimestampUtc\");");
                await ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS \"IX_ActivityLogs_GroupId_TimestampUtc\" ON \"ActivityLogs\" (\"GroupId\", \"TimestampUtc\");");
                await ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS \"IX_AuthSessions_UserId_RevokedAtUtc_LastActivityAtUtc\" ON \"AuthSessions\" (\"UserId\", \"RevokedAtUtc\", \"LastActivityAtUtc\");");
                await ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS \"IX_AuthSessions_UserId_RemoteIpAddress_CreatedAtUtc\" ON \"AuthSessions\" (\"UserId\", \"RemoteIpAddress\", \"CreatedAtUtc\");");
                await ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS \"IX_ChatMessages_ConversationKey_TimestampUtc\" ON \"ChatMessages\" (\"ConversationKey\", \"TimestampUtc\");");
                await ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS \"IX_ChatMessages_SenderUserId_RecipientUserId_TimestampUtc\" ON \"ChatMessages\" (\"SenderUserId\", \"RecipientUserId\", \"TimestampUtc\");");
                await ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS \"IX_Missions_Date_ClientId_TruckId\" ON \"Missions\" (\"Date\", \"ClientId\", \"TruckId\");");
                await ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS \"IX_UserRoles_RoleId_UserId\" ON \"UserRoles\" (\"RoleId\", \"UserId\");");
                await ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS \"IX_RolePermissions_PermissionId_RoleId\" ON \"RolePermissions\" (\"PermissionId\", \"RoleId\");");
                await ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS \"IX_UserObservations_UserId_IsActive_LastDetectedAtUtc\" ON \"UserObservations\" (\"UserId\", \"IsActive\", \"LastDetectedAtUtc\");");
            }
            finally
            {
                if (shouldCloseConnection)
                {
                    await connection.CloseAsync();
                }
            }
        }

        public static async Task EnsureSecurityPolicyDefaultsAsync(ApplicationDbContext dbContext)
        {
            var requiredPermissions = new[]
            {
                new AppPermission { Name = PermissionNames.MissionsManage, Description = "Manage orders and operational data." },
                new AppPermission { Name = PermissionNames.ChatUse, Description = "Use the real-time chat." },
                new AppPermission { Name = PermissionNames.LogsView, Description = "View recent activity logs." },
                new AppPermission { Name = PermissionNames.ObservationsView, Description = "View suspicious-user observations." },
                new AppPermission { Name = PermissionNames.SecurityLabManage, Description = "Manage security benchmarking and load generation." }
            };

            var permissionsByName = (await dbContext.Permissions.ToListAsync())
                .ToDictionary(permission => permission.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var permission in requiredPermissions)
            {
                if (permissionsByName.ContainsKey(permission.Name))
                {
                    continue;
                }

                dbContext.Permissions.Add(permission);
                permissionsByName[permission.Name] = permission;
            }

            if (dbContext.ChangeTracker.HasChanges())
            {
                await dbContext.SaveChangesAsync();
                permissionsByName = (await dbContext.Permissions.ToListAsync())
                    .ToDictionary(permission => permission.Name, StringComparer.OrdinalIgnoreCase);
            }

            var adminRole = await dbContext.Roles.SingleOrDefaultAsync(role => role.Name == RoleNames.Admin);
            var userRole = await dbContext.Roles.SingleOrDefaultAsync(role => role.Name == RoleNames.User);
            if (adminRole == null || userRole == null)
            {
                return;
            }

            var existingMappings = (await dbContext.RolePermissions
                .Select(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId })
                .ToListAsync())
                .Select(rolePermission => $"{rolePermission.RoleId}:{rolePermission.PermissionId}")
                .ToHashSet(StringComparer.Ordinal);

            EnsureRolePermission(dbContext, existingMappings, adminRole.Id, permissionsByName[PermissionNames.MissionsManage].Id);
            EnsureRolePermission(dbContext, existingMappings, adminRole.Id, permissionsByName[PermissionNames.ChatUse].Id);
            EnsureRolePermission(dbContext, existingMappings, adminRole.Id, permissionsByName[PermissionNames.LogsView].Id);
            EnsureRolePermission(dbContext, existingMappings, adminRole.Id, permissionsByName[PermissionNames.ObservationsView].Id);
            EnsureRolePermission(dbContext, existingMappings, adminRole.Id, permissionsByName[PermissionNames.SecurityLabManage].Id);
            EnsureRolePermission(dbContext, existingMappings, userRole.Id, permissionsByName[PermissionNames.ChatUse].Id);

            if (dbContext.ChangeTracker.HasChanges())
            {
                await dbContext.SaveChangesAsync();
            }
        }

        public static async Task EnsureUserSecurityDefaultsAsync(ApplicationDbContext dbContext)
        {
            var users = await dbContext.Users
                .OrderBy(user => user.Id)
                .ToListAsync();

            var changedUsers = new List<string>();

            foreach (var user in users)
            {
                if (string.IsNullOrWhiteSpace(user.SecurityCodeHash))
                {
                    var defaultSecurityCode = string.Equals(user.Email, ReservedAccountEmails.PrimaryAdmin, StringComparison.OrdinalIgnoreCase)
                        ? "246810"
                        : "135790";

                    user.SecurityCodeHash = CredentialHasher.HashSecurityCode(defaultSecurityCode);
                    changedUsers.Add($"Security code for {user.Email}: {defaultSecurityCode}");
                }

                if (string.IsNullOrWhiteSpace(user.AuthenticationPhraseHash))
                {
                    var defaultAuthenticationPhrase = string.Equals(user.Email, ReservedAccountEmails.PrimaryAdmin, StringComparison.OrdinalIgnoreCase)
                        ? DefaultAdminAuthenticationPhrase
                        : DefaultUserAuthenticationPhrase;

                    user.AuthenticationPhraseHash = CredentialHasher.HashAuthenticationPhrase(defaultAuthenticationPhrase);
                    changedUsers.Add($"Authentication phrase for {user.Email}: {defaultAuthenticationPhrase}");
                }
            }

            if (changedUsers.Count > 0)
            {
                await dbContext.SaveChangesAsync();

                Console.WriteLine("Bootstraped security credentials for existing users:");
                foreach (var message in changedUsers)
                {
                    Console.WriteLine($"  - {message}");
                }
            }
        }

        private static async Task EnsureColumn(DbConnection connection, string tableName, string columnName, string definition)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            await reader.DisposeAsync();
            await ExecuteNonQuery(connection, $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {definition};");
        }

        private static async Task ExecuteNonQuery(DbConnection connection, string sql)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }

        private static void EnsureRolePermission(
            ApplicationDbContext dbContext,
            ISet<string> existingMappings,
            int roleId,
            int permissionId)
        {
            if (!existingMappings.Add($"{roleId}:{permissionId}"))
            {
                return;
            }

            dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            });
        }
    }
}
