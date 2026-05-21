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
        public DbSet<UserObservation> UserObservations { get; set; }

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

            modelBuilder.Entity<Mission>()
                .Property(mission => mission.Cost)
                .HasPrecision(18, 2);

            modelBuilder.Entity<UserRole>()
                .HasKey(userRole => new { userRole.UserId, userRole.RoleId });

            modelBuilder.Entity<RolePermission>()
                .HasKey(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId });

            modelBuilder.Entity<ActivityLog>()
                .HasOne(activityLog => activityLog.User)
                .WithMany(user => user.ActivityLogs)
                .HasForeignKey(activityLog => activityLog.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<UserObservation>()
                .HasOne(observation => observation.User)
                .WithMany(user => user.Observations)
                .HasForeignKey(observation => observation.UserId)
                .OnDelete(DeleteBehavior.Cascade);

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
                    Date = new DateTime(2026, 5, 10)
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
                    Date = new DateTime(2026, 5, 1)
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
                    Date = new DateTime(2026, 5, 5)
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
                    Date = new DateTime(2026, 5, 15)
                }
            );

            modelBuilder.Entity<AppRole>().HasData(
                new AppRole { Id = 1, Name = RoleNames.Admin, Description = "Administrator with full permissions." },
                new AppRole { Id = 2, Name = RoleNames.User, Description = "Standard user with limited permissions." }
            );

            modelBuilder.Entity<AppPermission>().HasData(
                new AppPermission { Id = 1, Name = PermissionNames.MissionsManage, Description = "Manage orders and operational data." },
                new AppPermission { Id = 2, Name = PermissionNames.ChatUse, Description = "Use the real-time chat." },
                new AppPermission { Id = 3, Name = PermissionNames.ObservationsView, Description = "View suspicious users." },
                new AppPermission { Id = 4, Name = PermissionNames.LogsView, Description = "View recent activity logs." }
            );

            modelBuilder.Entity<AppUser>().HasData(
                new AppUser
                {
                    Id = 1,
                    FullName = "Administrator TDP",
                    Email = "admin@tdptrans.ro",
                    PasswordHash = CredentialHasher.HashPassword("12345678"),
                    IsActive = true,
                    CreatedAtUtc = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc)
                },
                new AppUser
                {
                    Id = 2,
                    FullName = "Sofer TDP",
                    Email = "sofer@tdptrans.ro",
                    PasswordHash = CredentialHasher.HashPassword("12345678"),
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
                new RolePermission { RoleId = 1, PermissionId = 3 },
                new RolePermission { RoleId = 1, PermissionId = 4 },
                new RolePermission { RoleId = 2, PermissionId = 2 }
            );
        }
    }
}
