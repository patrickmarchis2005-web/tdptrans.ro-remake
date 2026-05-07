using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
        }
    }
}
