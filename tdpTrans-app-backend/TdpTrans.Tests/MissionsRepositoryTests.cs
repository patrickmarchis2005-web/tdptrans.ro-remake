using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TdpTrans.Models;
using TdpTrans.Repositories;
using TdpTrans.Repositories.Implementations;
using Xunit;

namespace TdpTrans.Tests
{
    public class MissionsRepositoryTests
    {
        private async Task<ApplicationDbContext> GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task AddMission_ValidMission_MissionIsSavedToDatabase()
        {
            // Arrange
            var context = await GetDbContext();
            var repository = new MissionsRepository(context);

            var newMission = new Mission
            {
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Cost = 500,
                Address = "Strada Test 1",
                Date = DateTime.Now,
                ClientId = 1,
                TruckId = 123456
            };

            // Act
            await repository.AddMission(newMission);

            // Assert
            var missionInDb = await context.Missions.FirstOrDefaultAsync(m => m.Cost == 500);

            Assert.NotNull(missionInDb);
            Assert.Equal("Strada Test 1", missionInDb.Address);
            Assert.NotEqual(0, missionInDb.Id);
        }

        [Fact]
        public async Task GetAllMissions_ExistingMissions_ReturnsAllMissions()
        {
            // Arrange
            var context = await GetDbContext();

            context.Missions.Add(new Mission { Type = MissionType.Transport, Status = MissionStatus.Programata, Address = "A1", Cost = 100 });
            context.Missions.Add(new Mission { Type = MissionType.Tractare, Status = MissionStatus.Finalizata, Address = "A2", Cost = 200 });
            await context.SaveChangesAsync();

            var repository = new MissionsRepository(context);

            // Act
            var result = await repository.GetAllMissions(null);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(6, result.Count()); // eu la crearea db-ului, imi adaug 4 comenzi automat => 4+2=6
        }

        [Fact]
        public async Task GetAllMissions_SearchTerm_ReturnsFilteredMissions()
        {
            // Arrange
            var context = await GetDbContext();

            var client1 = new Client { Id = 101, Name = "Ion Pop", Email = "ion@test.ro", Phone = "0712345678" };
            var client2 = new Client { Id = 102, Name = "Maria Radu", Email = "maria@companie.com", Phone = "0798765432" };

            var mission1 = new Mission { Id = 5, ClientId = 101, Address = "A1", Cost = 100 };
            var mission2 = new Mission { Id = 6, ClientId = 102, Address = "A2", Cost = 200 };
            var mission3 = new Mission { Id = 7, ClientId = 101, Address = "A3", Cost = 300 };

            context.Clients.AddRange(client1, client2);
            context.Missions.AddRange(mission1, mission2, mission3);
            await context.SaveChangesAsync();

            var repository = new MissionsRepository(context);

            // Act
            var result = await repository.GetAllMissions("maria");

            // Assert
            var resultList = result.ToList();
            Assert.Single(resultList);
            Assert.Equal(6, resultList.First().Id);
        }

        [Fact]
        public async Task GetMissionById_ExistingMissionId_ReturnsCorrectMission()
        {
            // Arrange
            var context = await GetDbContext();

            var expectedMission = new Mission
            {
                Id = 99,
                Type = MissionType.Tractare,
                Status = MissionStatus.In_desfasurare,
                Cost = 750,
                Address = "Strada Cautarii 2"
            };

            context.Missions.Add(expectedMission);
            await context.SaveChangesAsync();

            var repository = new MissionsRepository(context);

            // Act
            var result = await repository.GetMissionById(99);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(99, result.Id);
            Assert.Equal(750, result.Cost);
        }

        [Fact]
        public async Task DeleteMission_ExistingMission_MissionIsRemovedFromDatabase()
        {
            // Arrange
            var context = await GetDbContext();
            var missionToDelete = new Mission
            {
                Id = 50,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Cost = 100,
                Address = "De sters"
            };

            context.Missions.Add(missionToDelete);
            await context.SaveChangesAsync();

            var repository = new MissionsRepository(context);

            // Act
            await repository.DeleteMission(missionToDelete);

            // Assert
            var missionInDb = await context.Missions.FirstOrDefaultAsync(m => m.Id == 50);

            Assert.Null(missionInDb);
        }
    }
}