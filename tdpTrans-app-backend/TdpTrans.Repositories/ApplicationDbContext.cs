using Microsoft.EntityFrameworkCore;
using TdpTrans.Models;

namespace TdpTrans.Repositories
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Truck> Trucks { get; set; }
        public DbSet<Mission> Missions { get; set; }
        public DbSet<AppUser> Users { get; set; }
        public DbSet<AppRole> Roles { get; set; }
        public DbSet<AppPermission> Permissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<AuthSession> AuthSessions { get; set; }
        public DbSet<UserObservation> UserObservations { get; set; }
        public DbSet<ChatMessageDocument> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppUser>()
                .HasIndex(user => user.Email)
                .IsUnique();

            modelBuilder.Entity<AppRole>()
                .HasIndex(role => role.Name)
                .IsUnique();

            modelBuilder.Entity<AppPermission>()
                .HasIndex(permission => permission.Name)
                .IsUnique();

            modelBuilder.Entity<AuthSession>()
                .HasIndex(session => session.TokenHash)
                .IsUnique();

            modelBuilder.Entity<ActivityLog>()
                .HasIndex(activityLog => new { activityLog.UserId, activityLog.ActionType, activityLog.TimestampUtc });

            modelBuilder.Entity<ActivityLog>()
                .HasIndex(activityLog => new { activityLog.GroupId, activityLog.TimestampUtc });

            modelBuilder.Entity<AuthSession>()
                .HasIndex(session => new { session.UserId, session.RevokedAtUtc, session.LastActivityAtUtc });

            modelBuilder.Entity<AuthSession>()
                .HasIndex(session => new { session.UserId, session.RemoteIpAddress, session.CreatedAtUtc });

            modelBuilder.Entity<ChatMessageDocument>()
                .HasIndex(message => new { message.ConversationKey, message.TimestampUtc });

            modelBuilder.Entity<ChatMessageDocument>()
                .HasIndex(message => new { message.SenderUserId, message.RecipientUserId, message.TimestampUtc });

            modelBuilder.Entity<Mission>()
                .Property(mission => mission.Cost)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Mission>()
                .HasIndex(mission => new { mission.Date, mission.ClientId, mission.TruckId });

            modelBuilder.Entity<UserRole>()
                .HasKey(userRole => new { userRole.UserId, userRole.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasIndex(userRole => new { userRole.RoleId, userRole.UserId });

            modelBuilder.Entity<RolePermission>()
                .HasKey(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId });

            modelBuilder.Entity<RolePermission>()
                .HasIndex(rolePermission => new { rolePermission.PermissionId, rolePermission.RoleId });

            modelBuilder.Entity<ActivityLog>()
                .HasOne(activityLog => activityLog.User)
                .WithMany(user => user.ActivityLogs)
                .HasForeignKey(activityLog => activityLog.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AuthSession>()
                .HasOne(session => session.User)
                .WithMany(user => user.Sessions)
                .HasForeignKey(session => session.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserObservation>()
                .HasOne(observation => observation.User)
                .WithMany()
                .HasForeignKey(observation => observation.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserObservation>()
                .HasIndex(observation => new { observation.UserId, observation.IsActive, observation.LastDetectedAtUtc });

            var client1 = new Client { Id = 1, Name = "Tech Logistics SRL", Phone = "0722111222", Email = "contact@techlog.ro" };
            var client2 = new Client { Id = 2, Name = "Auto Depanare SA", Phone = "0733444555", Email = "office@autodepanare.ro" };
            var client3 = new Client { Id = 3, Name = "Ion Popescu", Phone = "0744999888", Email = "ion.popescu@gmail.com" };
            modelBuilder.Entity<Client>().HasData(client1, client2, client3);

            var truck1 = new Truck { Id = 123456, LicensePlate = "CJ 99 TEST" };
            var truck2 = new Truck { Id = 654321, LicensePlate = "B 101 DEV" };
            var truck3 = new Truck { Id = 112233, LicensePlate = "TM 50 RAP" };
            modelBuilder.Entity<Truck>().HasData(truck1, truck2, truck3);

            modelBuilder.Entity<Mission>().HasData(
                new Mission
                {
                    Id = 1,
                    ClientId = 1,
                    TruckId = 123456,
                    Type = MissionType.Transport,
                    Status = MissionStatus.Programata,
                    Cost = 1500.00m,
                    Address = "Strada Lunga 10, Cluj-Napoca",
                    Date = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc)
                },
                new Mission
                {
                    Id = 2,
                    ClientId = 2,
                    TruckId = 654321,
                    Type = MissionType.Tractare,
                    Status = MissionStatus.Finalizata,
                    Cost = 450.50m,
                    Address = "Autostrada A3, km 25",
                    Date = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Mission
                {
                    Id = 3,
                    ClientId = 3,
                    TruckId = 112233,
                    Type = MissionType.Transport,
                    Status = MissionStatus.In_desfasurare,
                    Cost = 3200.00m,
                    Address = "Bulevardul Unirii, Bucuresti",
                    Date = new DateTime(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc)
                },
                new Mission
                {
                    Id = 4,
                    ClientId = 1,
                    TruckId = 123456,
                    Type = MissionType.Transport,
                    Status = MissionStatus.Programata,
                    Cost = 800.00m,
                    Address = "Soseaua Vestului, Ploiesti",
                    Date = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            modelBuilder.Entity<AppRole>().HasData(
                new AppRole { Id = 1, Name = RoleNames.Admin, Description = "Administrator with full permissions." },
                new AppRole { Id = 2, Name = RoleNames.User, Description = "Standard user with limited permissions." }
            );

            modelBuilder.Entity<AppPermission>().HasData(
                new AppPermission { Id = 1, Name = PermissionNames.MissionsManage, Description = "Manage orders and operational data." },
                new AppPermission { Id = 2, Name = PermissionNames.ChatUse, Description = "Use the real-time chat." },
                new AppPermission { Id = 4, Name = PermissionNames.LogsView, Description = "View recent activity logs." },
                new AppPermission { Id = 5, Name = PermissionNames.ObservationsView, Description = "View suspicious-user observations." },
                new AppPermission { Id = 6, Name = PermissionNames.SecurityLabManage, Description = "Manage security benchmarking and load generation." }
            );

            modelBuilder.Entity<AppUser>().HasData(
                new AppUser
                {
                    Id = 1,
                    FullName = "Administrator TDP",
                    Email = "admin@tdptrans.ro",
                    PasswordHash = CredentialHasher.HashPassword("12345678"),
                    SecurityCodeHash = CredentialHasher.HashSecurityCode("246810"),
                    AuthenticationPhraseHash = CredentialHasher.HashAuthenticationPhrase("TDP-ADMIN"),
                    IsActive = true,
                    CreatedAtUtc = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc)
                },
                new AppUser
                {
                    Id = 2,
                    FullName = "Sofer TDP",
                    Email = "sofer@tdptrans.ro",
                    PasswordHash = CredentialHasher.HashPassword("12345678"),
                    SecurityCodeHash = CredentialHasher.HashSecurityCode("135790"),
                    AuthenticationPhraseHash = CredentialHasher.HashAuthenticationPhrase("TDP-USER"),
                    IsActive = true,
                    CreatedAtUtc = new DateTime(2026, 5, 1, 8, 5, 0, DateTimeKind.Utc)
                }
            );

            modelBuilder.Entity<UserRole>().HasData(
                new UserRole { UserId = 1, RoleId = 1 },
                new UserRole { UserId = 2, RoleId = 2 }
            );

            modelBuilder.Entity<RolePermission>().HasData(
                new RolePermission { RoleId = 1, PermissionId = 1 },
                new RolePermission { RoleId = 1, PermissionId = 2 },
                new RolePermission { RoleId = 1, PermissionId = 4 },
                new RolePermission { RoleId = 1, PermissionId = 5 },
                new RolePermission { RoleId = 1, PermissionId = 6 },
                new RolePermission { RoleId = 2, PermissionId = 2 }
            );
        }
    }
}
