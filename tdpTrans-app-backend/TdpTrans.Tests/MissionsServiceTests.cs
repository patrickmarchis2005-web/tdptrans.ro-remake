using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Numerics;
using System.Threading.Tasks;
using TdpTrans.DTOs;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;
using TdpTrans.Services;
using Xunit;

namespace TdpTrans.Tests
{
    public class MissionsServiceTests
    {
        private readonly Mock<IMissionsRepository> _mockMissionsRepo;
        private readonly Mock<ITrucksRepository> _mockTrucksRepo;
        private readonly Mock<IClientsRepository> _mockClientsRepo;
        private readonly MissionsService _service;

        public MissionsServiceTests()
        {
            _mockMissionsRepo = new Mock<IMissionsRepository>();
            _mockClientsRepo = new Mock<IClientsRepository>();
            _mockTrucksRepo = new Mock<ITrucksRepository>();
            _service = new MissionsService(_mockMissionsRepo.Object, _mockClientsRepo.Object, _mockTrucksRepo.Object);
        }

        [Fact]
        public async Task AddMission_WithInvalidType_ThrowsArgumentException()
        {
            // Arrange
            var request = new CreateMissionRequest
            (
                "TipInexistent",
                100400,
                new DateTime(2026, 5, 1),
                100m,
                "Client Nou",
                "0705123456",
                "Str. Campului nr. 2",
                "client@yahoo.com",
                "Programata"
            );

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.AddMission(request));
            Assert.Contains($"Invalid mission type: {request.MissionType}", exception.Message);
        }

        [Fact]
        public async Task AddMission_WithInvalidStatus_ThrowsArgumentException()
        {
            // Arrange
            var request = new CreateMissionRequest
            (
                "Transport",
                100400,
                new DateTime(2026, 5, 1),
                100m,
                "Client Nou",
                "0705123456",
                "Str. Campului nr. 2",
                "client@yahoo.com",
                "StatusInexistentGresit"
            );

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.AddMission(request));
            Assert.Contains($"Invalid mission status: {request.MissionStatus}", exception.Message);
        }

        [Fact]
        public async Task AddMission_WithValidData_ReturnsNewId()
        {
            // Arrange
            var mission1 = new Mission
            {
                Id = 1,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 1),
                Cost = 100m,
                Address = "Str. Campului nr. 2",
                ClientId = 101,
                TruckId = 100400
            };
            var mission2 = new Mission
            {
                Id = 2,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 200m,
                Address = "Str. Campului nr. 3",
                ClientId = 102,
                TruckId = 100401
            };
            var expectedMissionResult = new Mission
            {
                Id = 3,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 1),
                Cost = 300m,
                Address = "Str. Campului nr. 3",
                ClientId = 101,
                TruckId = 100400
            };
            var existingMissions = new List<Mission>();
            existingMissions.AddRange(mission1, mission2);


            var truck1 = new Truck
            {
                Id = 100400,
                LicensePlate = "CJ 10 AAA"
            };
            var truck2 = new Truck
            {
                Id = 100401,
                LicensePlate = "CJ 11 AAA"
            };
            var existingTrucks = new List<Truck>();
            existingTrucks.AddRange(truck1, truck2);


            var client1 = new Client
            {
                Id = 101,
                Name = "Client 1",
                Phone = "0705123456",
                Email = "client1@gmail.com"
            };
            var client2 = new Client
            {
                Id = 102,
                Name = "Client 2",
                Phone = "0705123457",
                Email = "client2@gmail.com"
            };
            var existingClients = new List<Client>();
            existingClients.AddRange(client1, client2);


            _mockClientsRepo.Setup(repo => repo.GetClientByEmail("client1@gmail.com")).ReturnsAsync(client1);
            _mockTrucksRepo.Setup(repo => repo.GetTruckById(100400)).ReturnsAsync(truck1);
            _mockMissionsRepo.Setup(repo => repo.AddMission(It.IsAny<Mission>())).ReturnsAsync(expectedMissionResult);

            var request = new CreateMissionRequest
            (
                "Transport",
                100400,
                new DateTime(2026, 5, 1),
                300m,
                "Client 1",
                "0705123456",
                "Str. Campului nr. 3",
                "client1@gmail.com",
                "Programata"
            );

            // Act
            var newId = await _service.AddMission(request);

            // Assert
            Assert.Equal(3, newId);
            _mockMissionsRepo.Verify(repo => repo.AddMission(It.IsAny<Mission>()), Times.Once);
        }

        [Fact]
        public async Task AddMission_WhenRepoIsEmpty_ReturnsNewId()
        {
            // Arrange
            var existingMissions = new List<Mission>();

            var truck1 = new Truck
            {
                Id = 100400,
                LicensePlate = "CJ 10 AAA"
            };
            var truck2 = new Truck
            {
                Id = 100401,
                LicensePlate = "CJ 11 AAA"
            };
            var existingTrucks = new List<Truck>();
            existingTrucks.AddRange(truck1, truck2);


            var client1 = new Client
            {
                Id = 101,
                Name = "Client 1",
                Phone = "0705123456",
                Email = "client1@gmail.com"
            };
            var client2 = new Client
            {
                Id = 102,
                Name = "Client 2",
                Phone = "0705123457",
                Email = "client2@gmail.com"
            };
            var existingClients = new List<Client>();
            existingClients.AddRange(client1, client2);

            var expectedMissionResult = new Mission
            {
                Id = 1,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 1),
                Cost = 100m,
                Address = "Str. Campului nr. 2",
                ClientId = 101,
                TruckId = 100400
            };

            _mockClientsRepo.Setup(repo => repo.GetClientByEmail("client1@gmail.com")).ReturnsAsync(client1);
            _mockTrucksRepo.Setup(repo => repo.GetTruckById(100400)).ReturnsAsync(truck1);
            _mockMissionsRepo.Setup(repo => repo.AddMission(It.IsAny<Mission>())).ReturnsAsync(expectedMissionResult);

            var request = new CreateMissionRequest
            (
                "Transport",
                100400,
                new DateTime(2026, 5, 1),
                300m,
                "Client 1",
                "0705123456",
                "Str. Campului nr. 2",
                "client1@gmail.com",
                "Programata"
            );

            // Act
            var newId = await _service.AddMission(request);

            // Assert
            Assert.Equal(1, newId);
            _mockMissionsRepo.Verify(repo => repo.AddMission(It.IsAny<Mission>()), Times.Once);
        }

        [Fact]
        public async Task DeleteMissionById_WithNegativeId_ThrowsArgumentException()
        {
            // Arrange
            int invalidId = -1;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.DeleteMissionById(invalidId));
            Assert.Contains("Mission ID must be a positive integer.", exception.Message);
            _mockMissionsRepo.Verify(repo => repo.DeleteMission(It.IsAny<Mission>()), Times.Never);
        }

        [Fact]
        public async Task DeleteMissionById_WithNonExistingId_ThrowsInvalidOperationException()
        {
            // Arrange
            int nonExistingId = 999;

            var mission1 = new Mission
            {
                Id = 1,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 1),
                Cost = 100m,
                Address = "Str. Campului nr. 2",
                ClientId = 101,
                TruckId = 100400
            };
            var mission2 = new Mission
            {
                Id = 2,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 200m,
                Address = "Str. Campului nr. 3",
                ClientId = 102,
                TruckId = 100401
            };
            var existingMissions = new List<Mission>();
            existingMissions.AddRange(mission1, mission2);
            _mockMissionsRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(existingMissions);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteMissionById(nonExistingId));
            Assert.Contains($"Mission with ID {nonExistingId} not found.", exception.Message);
            _mockMissionsRepo.Verify(repo => repo.DeleteMission(It.IsAny<Mission>()), Times.Never);
        }

        [Fact]
        public async Task DeleteMissionById_WithExistingId_DeletesMission()
        {
            // Arrange
            int existingId = 1;
            var mission1 = new Mission
            {
                Id = 1,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 1),
                Cost = 100m,
                Address = "Str. Campului nr. 2",
                ClientId = 101,
                TruckId = 100400
            };
            var mission2 = new Mission
            {
                Id = 2,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 200m,
                Address = "Str. Campului nr. 3",
                ClientId = 102,
                TruckId = 100401
            };
            var existingMissions = new List<Mission>();
            existingMissions.AddRange(mission1, mission2);
            _mockMissionsRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(existingMissions);
            _mockMissionsRepo.Setup(repo => repo.DeleteMission(It.IsAny<Mission>())).Returns(Task.CompletedTask);

            // Act
            await _service.DeleteMissionById(existingId);

            // Assert
            _mockMissionsRepo.Verify(repo => repo.DeleteMission(It.IsAny<Mission>()), Times.Once);
        }

        [Fact]
        public async Task GetAllMissions_ValidCase_ReturnsMappedMissions()
        {
            // Arrange
            var mission1 = new Mission
            {
                Id = 1,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 1),
                Cost = 100m,
                Address = "Str. Campului nr. 2",
                ClientId = 101,
                Client = new Client { Name = "Test Client1", Email = "test2@test.com" },
                TruckId = 100400,
                Truck = new Truck { LicensePlate = "CJ 99 TST" }
            };
            var mission2 = new Mission
            {
                Id = 2,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 200m,
                Address = "Str. Campului nr. 3",
                ClientId = 102,
                Client = new Client { Name = "Test Client2", Email = "test1@test.com" },
                TruckId = 100401,
                Truck = new Truck { LicensePlate = "CJ 98 TST" }
            };
            var existingMissions = new List<Mission>();
            existingMissions.AddRange(mission1, mission2);
            _mockMissionsRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(existingMissions);

            // Act
            var result = await _service.GetAllMissions();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _mockMissionsRepo.Verify(repo => repo.GetAllMissions(null), Times.Once);
        }

        [Fact]
        public async Task GetMissionById_WhenMissionExists_ReturnsCorrectMission()
        {
            // Arrange
            var mission1 = new Mission
            {
                Id = 1,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 1),
                Cost = 100m,
                Address = "Str. Campului nr. 2",
                ClientId = 101,
                Client = new Client { Name = "Test Client", Email = "test@test.com" },
                TruckId = 100400,
                Truck = new Truck { LicensePlate = "CJ 99 TST" }
            };
            var existingMissions = new List<Mission>();
            existingMissions.AddRange(mission1);
            _mockMissionsRepo.Setup(repo => repo.GetMissionById(1)).ReturnsAsync(mission1);

            // Act
            var result = await _service.GetMissionById(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Transport", result.MissionType);
        }

        [Fact]
        public async Task GetMissionById_WhenMissionDoesNotExist_ReturnsNull()
        {
            // Arrange
            _mockMissionsRepo
                .Setup(repo => repo.GetMissionById(100))
                .ReturnsAsync((Mission)null);

            // Act
            var result = await _service.GetMissionById(100);

            // Assert
            Assert.Null(result);
            _mockMissionsRepo.Verify(repo => repo.GetMissionById(100), Times.Once);
        }

        [Fact]
        public async Task GetMissionsPaginated_WithNegativePage_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.GetMissionsPaginated(page: 0, pageSize: 10));
            Assert.Contains("Page number must be a positive integer.", exception.Message);
        }

        [Fact]
        public async Task GetMissionsPaginated_WithNegativePageSize_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.GetMissionsPaginated(page: 1, pageSize: 0));
            Assert.Contains("Page size must be a positive integer.", exception.Message);
        }

        [Fact]
        public async Task GetMissionsPaginated_WithValidParameters_ReturnsPaginatedResult()
        {
            // Arrange
            var mission1 = new Mission
            {
                Id = 1,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 1),
                Cost = 100m,
                Address = "Str. Campului nr. 2",
                ClientId = 101,
                Client = new Client { Name = "Test Client1", Email = "test1@test.com" },
                TruckId = 100400,
                Truck = new Truck { LicensePlate = "CJ 99 TST" }
            };
            var mission2 = new Mission
            {
                Id = 2,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 200m,
                Address = "Str. Campului nr. 3",
                ClientId = 102,
                Client = new Client { Name = "Test Client2", Email = "test2@test.com" },
                TruckId = 100401,
                Truck = new Truck { LicensePlate = "CJ 98 TST" }
            };
            var mission3 = new Mission
            {
                Id = 3,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 300m,
                Address = "Str. Campului nr. 4",
                ClientId = 101,
                Client = new Client { Name = "Test Client1", Email = "test1@test.com" },
                TruckId = 100400,
                Truck = new Truck { LicensePlate = "CJ 99 TST" }
            };
            var existingMissions = new List<Mission>();
            existingMissions.AddRange(mission1, mission2, mission3);
            _mockMissionsRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(existingMissions);

            // Act
            var result = await _service.GetMissionsPaginated(page: 2, pageSize: 2);

            // Assert
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(2, result.CurrentPage);
            Assert.Single(result.Items);
        }

        [Fact]
        public async Task GetMissionStatistics_ValidCase_ReturnsCorrectCalculations()
        {
            // 1. ARRANGE
            var mission1 = new Mission
            {
                Id = 1,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 4, 1),
                Cost = 100m,
                Address = "Str. Campului nr. 2",
                ClientId = 101,
                TruckId = 100400
            };
            var mission2 = new Mission
            {
                Id = 2,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 200m,
                Address = "Str. Campului nr. 3",
                ClientId = 102,
                TruckId = 100401
            };
            var mission3 = new Mission
            {
                Id = 3,
                Type = MissionType.Tractare,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 300m,
                Address = "Str. Campului nr. 4",
                ClientId = 101,
                TruckId = 100400
            };
            var existingMissions = new List<Mission>();
            existingMissions.AddRange(mission1, mission2, mission3);
            _mockMissionsRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(existingMissions);

            // Act
            var stats = await _service.GetMissionStatistics();

            // Assert
            Assert.Equal(3, stats.TotalComenzi);
            Assert.Equal(2, stats.TotalTransportMarfa);
            Assert.Equal(1, stats.TotalTractari);

            Assert.NotNull(stats.MonthlyData);
            var mayStats = stats.MonthlyData.FirstOrDefault(m => m.Name == "May");
            Assert.NotNull(mayStats);
            Assert.Equal(2, mayStats.Transport + mayStats.Towing);
        }

        [Fact]
        public async Task UpdateMissionById_WithNonExistingId_ThrowsArgumentException()
        {
            // Arrange
            int nonExistingId = 999;
            var request = new UpdateMissionRequest("Transport", null, null, null, null, null, null, null, null);
            var mission1 = new Mission
            {
                Id = 1,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 4, 1),
                Cost = 100m,
                Address = "Str. Campului nr. 2",
                ClientId = 101,
                TruckId = 100400
            };
            var mission2 = new Mission
            {
                Id = 2,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 200m,
                Address = "Str. Campului nr. 3",
                ClientId = 102,
                TruckId = 100401
            };
            var mission3 = new Mission
            {
                Id = 3,
                Type = MissionType.Tractare,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 300m,
                Address = "Str. Campului nr. 4",
                ClientId = 101,
                TruckId = 100400
            };
            var existingMissions = new List<Mission>();
            existingMissions.AddRange(mission1, mission2, mission3);
            _mockMissionsRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(existingMissions);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateMissionById(nonExistingId, request));
            Assert.Contains($"Mission with ID {nonExistingId} not found.", exception.Message);
        }

        [Fact]
        public async Task UpdateMissionById_WithInvalidMissionType_ThrowsArgumentException()
        {
            // Arrange
            int missionId = 1;
            var request = new UpdateMissionRequest("InvalidType", null, null, null, null, null, null, null, null);
            var mission1 = new Mission
            {
                Id = 1,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 4, 1),
                Cost = 100m,
                Address = "Str. Campului nr. 2",
                ClientId = 101,
                TruckId = 100400
            };
            var mission2 = new Mission
            {
                Id = 2,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 200m,
                Address = "Str. Campului nr. 3",
                ClientId = 102,
                TruckId = 100401
            };
            var mission3 = new Mission
            {
                Id = 3,
                Type = MissionType.Tractare,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 300m,
                Address = "Str. Campului nr. 4",
                ClientId = 101,
                TruckId = 100400
            };
            var existingMissions = new List<Mission>();
            existingMissions.AddRange(mission1, mission2, mission3);
            _mockMissionsRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(existingMissions);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateMissionById(missionId, request));
            Assert.Contains($"Invalid mission type: {request.MissionType}", exception.Message);
            _mockMissionsRepo.Verify(repo => repo.GetAllMissions(null), Times.Once);
        }

        [Fact]
        public async Task UpdateMissionById_WithInvalidMissionStatus_ThrowsArgumentException()
        {
            // Arrange
            int missionId = 1;
            var request = new UpdateMissionRequest("Transport", null, null, null, null, null, null, null, "InvalidStatus");
            var mission1 = new Mission
            {
                Id = 1,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 4, 1),
                Cost = 100m,
                Address = "Str. Campului nr. 2",
                ClientId = 101,
                TruckId = 100400
            };
            var mission2 = new Mission
            {
                Id = 2,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 200m,
                Address = "Str. Campului nr. 3",
                ClientId = 102,
                TruckId = 100401
            };
            var mission3 = new Mission
            {
                Id = 3,
                Type = MissionType.Tractare,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 300m,
                Address = "Str. Campului nr. 4",
                ClientId = 101,
                TruckId = 100400
            };
            var existingMissions = new List<Mission>();
            existingMissions.AddRange(mission1, mission2, mission3);
            _mockMissionsRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(existingMissions);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateMissionById(missionId, request));
            Assert.Contains($"Invalid mission status: {request.MissionStatus}", exception.Message);
            _mockMissionsRepo.Verify(repo => repo.GetAllMissions(null), Times.Once);
        }

        [Fact]
        public async Task UpdateMissionById_TruckIdHasValue_UpdatesMission()
        {
            // Arrange
            int missionId = 1;
            int newTruckId = 100500;
            var request = new UpdateMissionRequest(null, newTruckId, null, null, null, null, null, null, "Programata");
            var mission1 = new Mission
            {
                Id = 1,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 4, 1),
                Cost = 100m,
                Address = "Str. Campului nr. 2",
                ClientId = 101,
                TruckId = 100400
            };
            var mission2 = new Mission
            {
                Id = 2,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 200m,
                Address = "Str. Campului nr. 3",
                ClientId = 102,
                TruckId = 100401
            };
            var mission3 = new Mission
            {
                Id = 3,
                Type = MissionType.Tractare,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 300m,
                Address = "Str. Campului nr. 4",
                ClientId = 101,
                TruckId = 100400
            };
            var existingMissions = new List<Mission>();
            existingMissions.AddRange(mission1, mission2, mission3);
            _mockMissionsRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(existingMissions);

            // Act
            var updatedMissionId = await _service.UpdateMissionById(missionId, request);

            // Assert
            Assert.Equal(missionId, updatedMissionId);
            _mockMissionsRepo.Verify(repo => repo.GetAllMissions(null), Times.Once);
            Assert.Equal(newTruckId, existingMissions.First().TruckId);
        }

        [Fact]
        public async Task UpdateMissionById_DateHasValue_UpdatesMission()
        {
            // Arrange
            int missionId = 1;
            DateTime newDate = new DateTime(2026, 6, 1);
            var request = new UpdateMissionRequest(null, null, newDate, null, null, null, null, null, null);
            var mission1 = new Mission
            {
                Id = 1,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 4, 1),
                Cost = 100m,
                Address = "Str. Campului nr. 2",
                ClientId = 101,
                TruckId = 100400
            };
            var mission2 = new Mission
            {
                Id = 2,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 200m,
                Address = "Str. Campului nr. 3",
                ClientId = 102,
                TruckId = 100401
            };
            var mission3 = new Mission
            {
                Id = 3,
                Type = MissionType.Tractare,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 300m,
                Address = "Str. Campului nr. 4",
                ClientId = 101,
                TruckId = 100400
            };
            var existingMissions = new List<Mission>();
            existingMissions.AddRange(mission1, mission2, mission3);
            _mockMissionsRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(existingMissions);

            // Act
            var updatedMissionId = await _service.UpdateMissionById(missionId, request);

            // Assert
            Assert.Equal(missionId, updatedMissionId);
            _mockMissionsRepo.Verify(repo => repo.GetAllMissions(null), Times.Once);
            Assert.Equal(newDate, existingMissions.First().Date);
        }

        [Fact]
        public async Task UpdateMissionById_CostHasValue_UpdatesMission()
        {
            // Arrange
            int missionId = 1;
            decimal newCost = 200m;
            var request = new UpdateMissionRequest(null, null, null, newCost, null, null, null, null, null);
            var mission1 = new Mission
            {
                Id = 1,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 4, 1),
                Cost = 100m,
                Address = "Str. Campului nr. 2",
                ClientId = 101,
                TruckId = 100400
            };
            var mission2 = new Mission
            {
                Id = 2,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 200m,
                Address = "Str. Campului nr. 3",
                ClientId = 102,
                TruckId = 100401
            };
            var mission3 = new Mission
            {
                Id = 3,
                Type = MissionType.Tractare,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 300m,
                Address = "Str. Campului nr. 4",
                ClientId = 101,
                TruckId = 100400
            };
            var existingMissions = new List<Mission>();
            existingMissions.AddRange(mission1, mission2, mission3);
            _mockMissionsRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(existingMissions);

            // Act
            var updatedMissionId = await _service.UpdateMissionById(missionId, request);

            // Assert
            Assert.Equal(missionId, updatedMissionId);
            _mockMissionsRepo.Verify(repo => repo.GetAllMissions(null), Times.Once);
            Assert.Equal(newCost, existingMissions.First().Cost);
        }

        [Fact]
        public async Task UpdateMissionById_ClientNotNull_UpdatesMission()
        {
            // Arrange
            int missionId = 1;
            string newClient = "Client 2";
            var request = new UpdateMissionRequest(null, null, null, null, newClient, null, null, null, null);
            var client1 = new Client
            {
                Id = 100,
                Name = "Client 1"
            };
            var mission1 = new Mission
            {
                Id = 1,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 4, 1),
                Cost = 100m,
                Address = "Str. Campului nr. 2",
                ClientId = 101,
                TruckId = 100400,
                Client = client1
            };
            var mission2 = new Mission
            {
                Id = 2,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 200m,
                Address = "Str. Campului nr. 3",
                ClientId = 102,
                TruckId = 100401
            };
            var mission3 = new Mission
            {
                Id = 3,
                Type = MissionType.Tractare,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 300m,
                Address = "Str. Campului nr. 4",
                ClientId = 101,
                TruckId = 100400
            };
            var existingMissions = new List<Mission>();
            existingMissions.AddRange(mission1, mission2, mission3);
            _mockMissionsRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(existingMissions);

            // Act
            var updatedMissionId = await _service.UpdateMissionById(missionId, request);

            // Assert
            Assert.Equal(missionId, updatedMissionId);
            _mockMissionsRepo.Verify(repo => repo.GetAllMissions(null), Times.Once);
            Assert.Equal(newClient, existingMissions.First().Client.Name);
        }

        [Fact]
        public async Task UpdateMissionById_PhoneNotNull_UpdatesMission()
        {
            // Arrange
            int missionId = 1;
            string newPhone = "0712345678";
            var request = new UpdateMissionRequest(null, null, null, null, null, newPhone, null, null, null);
            var client1 = new Client
            {
                Id = 100,
                Phone = "0712123123"
            };
            var mission1 = new Mission
            {
                Id = 1,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 4, 1),
                Cost = 100m,
                Address = "Str. Campului nr. 2",
                ClientId = 101,
                TruckId = 100400,
                Client = client1
            };
            var mission2 = new Mission
            {
                Id = 2,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 200m,
                Address = "Str. Campului nr. 3",
                ClientId = 102,
                TruckId = 100401
            };
            var mission3 = new Mission
            {
                Id = 3,
                Type = MissionType.Tractare,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 300m,
                Address = "Str. Campului nr. 4",
                ClientId = 101,
                TruckId = 100400
            };
            var existingMissions = new List<Mission>();
            existingMissions.AddRange(mission1, mission2, mission3);
            _mockMissionsRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(existingMissions);

            // Act
            var updatedMissionId = await _service.UpdateMissionById(missionId, request);

            // Assert
            Assert.Equal(missionId, updatedMissionId);
            _mockMissionsRepo.Verify(repo => repo.GetAllMissions(null), Times.Once);
            Assert.Equal(newPhone, existingMissions.First().Client.Phone);
        }

        [Fact]
        public async Task UpdateMissionById_AddressHasValue_UpdatesMission()
        {
            // Arrange
            int missionId = 1;
            DateTime newDate = new DateTime(2026, 6, 1);
            var newAddress = "Str. Noua nr. 5";
            var request = new UpdateMissionRequest(null, null, null, null, null, null, newAddress, null, null);
            var mission1 = new Mission
            {
                Id = 1,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 4, 1),
                Cost = 100m,
                Address = "Str. Campului nr. 2",
                ClientId = 101,
                TruckId = 100400
            };
            var mission2 = new Mission
            {
                Id = 2,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 200m,
                Address = "Str. Campului nr. 3",
                ClientId = 102,
                TruckId = 100401
            };
            var mission3 = new Mission
            {
                Id = 3,
                Type = MissionType.Tractare,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 300m,
                Address = "Str. Campului nr. 4",
                ClientId = 101,
                TruckId = 100400
            };
            var existingMissions = new List<Mission>();
            existingMissions.AddRange(mission1, mission2, mission3);
            _mockMissionsRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(existingMissions);

            // Act
            var updatedMissionId = await _service.UpdateMissionById(missionId, request);

            // Assert
            Assert.Equal(missionId, updatedMissionId);
            _mockMissionsRepo.Verify(repo => repo.GetAllMissions(null), Times.Once);
            Assert.Equal(newAddress, existingMissions.First().Address);
        }

        [Fact]
        public async Task UpdateMissionById_EmailHasValue_UpdatesMission()
        {
            // Arrange
            int missionId = 1;
            var newEmail = "newemail@gmail.com";
            var request = new UpdateMissionRequest(null, null, null, null, null, null, null, newEmail, null);
            var client1 = new Client
            {
                Id = 100,
                Email = "oldemail@gmail.com"
            };
            var mission1 = new Mission
            {
                Id = 1,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 4, 1),
                Cost = 100m,
                Address = "Str. Campului nr. 2",
                ClientId = 101,
                Client = new Client { Name = "Test Client1", Email = "test1@test.com" },
                TruckId = 100400,
                Truck = new Truck { LicensePlate = "CJ 99 TST" }
            };
            var mission2 = new Mission
            {
                Id = 2,
                Type = MissionType.Transport,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 200m,
                Address = "Str. Campului nr. 3",
                ClientId = 102,
                Client = new Client { Name = "Test Client2", Email = "test2@test.com" },
                TruckId = 100401,
                Truck = new Truck { LicensePlate = "CJ 98 TST" }
            };
            var mission3 = new Mission
            {
                Id = 3,
                Type = MissionType.Tractare,
                Status = MissionStatus.Programata,
                Date = new DateTime(2026, 5, 2),
                Cost = 300m,
                Address = "Str. Campului nr. 4",
                ClientId = 101,
                Client = new Client { Name = "Test Client1", Email = "test1@test.com" },
                TruckId = 100400,
                Truck = new Truck { LicensePlate = "CJ 99 TST" }
            };
            var existingMissions = new List<Mission>();
            existingMissions.AddRange(mission1, mission2, mission3);
            _mockMissionsRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(existingMissions);

            // Act
            var updatedMissionId = await _service.UpdateMissionById(missionId, request);

            // Assert
            Assert.Equal(missionId, updatedMissionId);
            _mockMissionsRepo.Verify(repo => repo.GetAllMissions(null), Times.Once);
            Assert.Equal(newEmail, existingMissions.First().Client.Email);
        }
    }
}