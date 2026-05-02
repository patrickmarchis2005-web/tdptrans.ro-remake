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
using TdpTrans.Repositories;
using TdpTrans.Services;
using Xunit;

namespace TdpTrans.Tests
{
    public class MissionsServiceTests
    {
        private readonly Mock<IMissionsRepository> _mockRepo;
        private readonly MissionsService _service;

        public MissionsServiceTests()
        {
            _mockRepo = new Mock<IMissionsRepository>();
            _service = new MissionsService(_mockRepo.Object);
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
            var existingMissions = new List<Mission>
            {
                new(
                    1,
                    MissionType.Transport,
                    100400,
                    new DateTime(2026, 5, 1),
                    100m,
                    "Client 1",
                    "0705123456",
                    "Str. Campului nr. 2",
                    "client1@gmail.com",
                    MissionStatus.Programata
                ),
                new(
                    2,
                    MissionType.Transport,
                    100401,
                    new DateTime(2026, 5, 2),
                    200m,
                    "Client 2",
                    "0705123457",
                    "Str. Campului nr. 3",
                    "client2@gmail.com",
                    MissionStatus.Programata
                )
            };

            _mockRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(existingMissions);
            _mockRepo.Setup(repo => repo.AddMission(It.IsAny<Mission>())).Returns(Task.CompletedTask);

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
                "Programata"
            );

            // Act
            var newId = await _service.AddMission(request);

            // Assert
            Assert.Equal(3, newId);
            _mockRepo.Verify(repo => repo.AddMission(It.IsAny<Mission>()), Times.Once);
        }

        [Fact]
        public async Task AddMission_WhenRepoIsEmpty_ReturnsNewId()
        {
            // Arrange
            var existingMissions = new List<Mission>();

            _mockRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(existingMissions);
            _mockRepo.Setup(repo => repo.AddMission(It.IsAny<Mission>())).Returns(Task.CompletedTask);

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
                "Programata"
            );

            // Act
            var newId = await _service.AddMission(request);

            // Assert
            Assert.Equal(1, newId);
            _mockRepo.Verify(repo => repo.AddMission(It.IsAny<Mission>()), Times.Once);
        }

        [Fact]
        public async Task DeleteMissionById_WithNegaticeId_ThrowsArgumentException()
        {
            // Arrange
            int invalidId = -1;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.DeleteMissionById(invalidId));
            Assert.Contains("Mission ID must be a positive integer.", exception.Message);
            _mockRepo.Verify(repo => repo.DeleteMission(It.IsAny<Mission>()), Times.Never);
        }

        [Fact]
        public async Task DeleteMissionById_WithNonExistingId_ThrowsInvalidOperationException()
        {
            // Arrange
            int nonExistingId = 999;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteMissionById(nonExistingId));
            Assert.Contains($"Mission with ID {nonExistingId} not found.", exception.Message);
            _mockRepo.Verify(repo => repo.DeleteMission(It.IsAny<Mission>()), Times.Never);
        }

        [Fact]
        public async Task DeleteMissionById_WithExistingId_DeletesMission()
        {
            // Arrange
            int existingId = 1;
            var existingMission = new Mission
            (
                existingId,
                MissionType.Transport,
                100400,
                new DateTime(2026, 5, 1),
                100m,
                "Client 1",
                "0705123456",
                "Str. Campului nr. 2",
                "client1@gmail.com",
                MissionStatus.Programata
            );

            _mockRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(new List<Mission> { existingMission });
            _mockRepo.Setup(repo => repo.DeleteMission(existingMission)).Returns(Task.CompletedTask);

            // Act
            await _service.DeleteMissionById(existingId);

            // Assert
            _mockRepo.Verify(repo => repo.DeleteMission(existingMission), Times.Once);
        }

        [Fact]
        public async Task GetAllMissions_ValidCase_ReturnsMappedMissions()
        {
            // Arrange
            var fakeMissions = new List<Mission>
            {
                new(
                    1,
                    MissionType.Transport,
                    100400,
                    new DateTime(2026, 5, 1),
                    100m,
                    "Client 1",
                    "0705123456",
                    "Str. Campului nr. 2",
                    "client1@gmail.com",
                    MissionStatus.Programata
                ),
                new(
                    2,
                    MissionType.Transport,
                    100401,
                    new DateTime(2026, 5, 2),
                    200m,
                    "Client 2",
                    "0705123457",
                    "Str. Campului nr. 3",
                    "client2@gmail.com",
                    MissionStatus.Programata
                )
            };
            _mockRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(fakeMissions);

            // Act
            var result = await _service.GetAllMissions();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _mockRepo.Verify(repo => repo.GetAllMissions(null), Times.Once);
        }

        [Fact]
        public async Task GetMissionById_WhenMissionExists_ReturnsCorrectMission()
        {
            // Arrange
            var fakeMissions = new List<Mission>
            {
                new(
                    1,
                    MissionType.Transport,
                    100400,
                    new DateTime(2026, 5, 1),
                    100m,
                    "Client 1",
                    "0705123456",
                    "Str. Campului nr. 2",
                    "client1@gmail.com",
                    MissionStatus.Programata
                ),
                new(
                    2,
                    MissionType.Transport,
                    100401,
                    new DateTime(2026, 5, 2),
                    200m,
                    "Client 2",
                    "0705123457",
                    "Str. Campului nr. 3",
                    "client2@gmail.com",
                    MissionStatus.Programata
                )
            };
            _mockRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(fakeMissions);

            // Act
            var result = await _service.GetMissionById(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Client 1", result.Client);
        }

        [Fact]
        public async Task GetMissionById_WhenMissionDoesNotExist_ReturnsNull()
        {
            // Arrange
            var fakeMissions = new List<Mission>
            {
                new(
                    1,
                    MissionType.Transport,
                    100400,
                    new DateTime(2026, 5, 1),
                    100m,
                    "Client 1",
                    "0705123456",
                    "Str. Campului nr. 2",
                    "client1@gmail.com",
                    MissionStatus.Programata
                )
            };
            _mockRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(fakeMissions);

            // Act
            var result = await _service.GetMissionById(100);

            // Assert
            Assert.Null(result);
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
            var fakeMissions = new List<Mission>
            {
                new(
                    1,
                    MissionType.Transport,
                    100400,
                    new DateTime(2026, 5, 1),
                    100m,
                    "Client 1",
                    "0705123456",
                    "Str. Campului nr. 2",
                    "client1@gmail.com",
                    MissionStatus.Programata
                ),
                new(
                    2,
                    MissionType.Transport,
                    100401,
                    new DateTime(2026, 5, 2),
                    200m,
                    "Client 2",
                    "0705123457",
                    "Str. Campului nr. 3",
                    "client2@gmail.com",
                    MissionStatus.Programata
                ),
                new(
                    3,
                    MissionType.Transport,
                    100402,
                    new DateTime(2026, 5, 3),
                    300m,
                    "Client 3",
                    "0705123458",
                    "Str. Campului nr. 4",
                    "client3@gmail.com",
                    MissionStatus.Programata
                )
            };

            _mockRepo.Setup(repo => repo.GetAllMissions(It.IsAny<string>())).ReturnsAsync(fakeMissions);

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
            var fakeMissions = new List<Mission>
            {
                new(
                    1,
                    MissionType.Transport,
                    100400,
                    new DateTime(2026, 4, 1),
                    100m,
                    "Client 1",
                    "0705123456",
                    "Str. Campului nr. 2",
                    "client1@gmail.com",
                    MissionStatus.Programata
                ),
                new(
                    2,
                    MissionType.Transport,
                    100401,
                    new DateTime(2026, 5, 2),
                    200m,
                    "Client 2",
                    "0705123457",
                    "Str. Campului nr. 3",
                    "client2@gmail.com",
                    MissionStatus.Programata
                ),
                new(
                    3,
                    MissionType.Tractare,
                    100402,
                    new DateTime(2026, 5, 3),
                    300m,
                    "Client 3",
                    "0705123458",
                    "Str. Campului nr. 4",
                    "client3@gmail.com",
                    MissionStatus.Programata
                )
            };
            _mockRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(fakeMissions);

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
            _mockRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(new List<Mission>());

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
            var missions = new List<Mission>
            {
                new(
                    missionId,
                    MissionType.Transport,
                    100400,
                    new DateTime(2026, 5, 1),
                    100m,
                    "Client 1",
                    "0705123456",
                    "Str. Campului nr. 2",
                    "client1@gmail.com",
                    MissionStatus.Programata
                )
            };
            _mockRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(missions);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateMissionById(missionId, request));
            Assert.Contains($"Invalid mission type: {request.MissionType}", exception.Message);
            _mockRepo.Verify(repo => repo.GetAllMissions(null), Times.Once);
        }

        [Fact]
        public async Task UpdateMissionById_WithInvalidMissionStatus_ThrowsArgumentException()
        {
            // Arrange
            int missionId = 1;
            var request = new UpdateMissionRequest("Transport", null, null, null, null, null, null, null, "InvalidStatus");
            var missions = new List<Mission>
            {
                new(
                    missionId,
                    MissionType.Transport,
                    100400,
                    new DateTime(2026, 5, 1),
                    100m,
                    "Client 1",
                    "0705123456",
                    "Str. Campului nr. 2",
                    "client1@gmail.com",
                    MissionStatus.Programata
                )
            };
            _mockRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(missions);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateMissionById(missionId, request));
            Assert.Contains($"Invalid mission status: {request.MissionStatus}", exception.Message);
            _mockRepo.Verify(repo => repo.GetAllMissions(null), Times.Once);
        }

        [Fact]
        public async Task UpdateMissionById_TruckIdHasValue_UpdatesMission()
        {
            // Arrange
            int missionId = 1;
            int newTruckId = 100500;
            var request = new UpdateMissionRequest(null, newTruckId, null, null, null, null, null, null, "Programata");
            var missions = new List<Mission>
            {
                new(
                    missionId,
                    MissionType.Transport,
                    100400,
                    new DateTime(2026, 5, 1),
                    100m,
                    "Client 1",
                    "0705123456",
                    "Str. Campului nr. 2",
                    "client1@gmail.com",
                    MissionStatus.Programata
                )
            };
            _mockRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(missions);

            // Act
            var updatedMissionId = await _service.UpdateMissionById(missionId, request);

            // Assert
            Assert.Equal(missionId, updatedMissionId);
            _mockRepo.Verify(repo => repo.GetAllMissions(null), Times.Once);
            Assert.Equal(newTruckId, missions.First().TruckId);
        }

        [Fact]
        public async Task UpdateMissionById_DateHasValue_UpdatesMission()
        {
            // Arrange
            int missionId = 1;
            DateTime newDate = new DateTime(2026, 6, 1);
            var request = new UpdateMissionRequest(null, null, newDate, null, null, null, null, null, null);
            var missions = new List<Mission>
            {
                new(
                    missionId,
                    MissionType.Transport,
                    100400,
                    new DateTime(2026, 5, 1),
                    100m,
                    "Client 1",
                    "0705123456",
                    "Str. Campului nr. 2",
                    "client1@gmail.com",
                    MissionStatus.Programata
                )
            };
            _mockRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(missions);

            // Act
            var updatedMissionId = await _service.UpdateMissionById(missionId, request);

            // Assert
            Assert.Equal(missionId, updatedMissionId);
            _mockRepo.Verify(repo => repo.GetAllMissions(null), Times.Once);
            Assert.Equal(newDate, missions.First().Date);
        }

        [Fact]
        public async Task UpdateMissionById_CostHasValue_UpdatesMission()
        {
            // Arrange
            int missionId = 1;
            decimal newCost = 200m;
            var request = new UpdateMissionRequest(null, null, null, newCost, null, null, null, null, null);
            var missions = new List<Mission>
            {
                new(
                    missionId,
                    MissionType.Transport,
                    100400,
                    new DateTime(2026, 5, 1),
                    100m,
                    "Client 1",
                    "0705123456",
                    "Str. Campului nr. 2",
                    "client1@gmail.com",
                    MissionStatus.Programata
                )
            };
            _mockRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(missions);

            // Act
            var updatedMissionId = await _service.UpdateMissionById(missionId, request);

            // Assert
            Assert.Equal(missionId, updatedMissionId);
            _mockRepo.Verify(repo => repo.GetAllMissions(null), Times.Once);
            Assert.Equal(newCost, missions.First().Cost);
        }

        [Fact]
        public async Task UpdateMissionById_ClientNotNull_UpdatesMission()
        {
            // Arrange
            int missionId = 1;
            string newClient = "Client 2";
            var request = new UpdateMissionRequest(null, null, null, null, newClient, null, null, null, null);
            var missions = new List<Mission>
            {
                new(
                    missionId,
                    MissionType.Transport,
                    100400,
                    new DateTime(2026, 5, 1),
                    100m,
                    "Client 1",
                    "0705123456",
                    "Str. Campului nr. 2",
                    "client1@gmail.com",
                    MissionStatus.Programata
                )
            };
            _mockRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(missions);

            // Act
            var updatedMissionId = await _service.UpdateMissionById(missionId, request);

            // Assert
            Assert.Equal(missionId, updatedMissionId);
            _mockRepo.Verify(repo => repo.GetAllMissions(null), Times.Once);
            Assert.Equal(newClient, missions.First().Client);
        }

        [Fact]
        public async Task UpdateMissionById_PhoneNotNull_UpdatesMission()
        {
            // Arrange
            int missionId = 1;
            string newPhone = "0712345678";
            var request = new UpdateMissionRequest(null, null, null, null, null, newPhone, null, null, null);
            var missions = new List<Mission>
            {
                new(
                    missionId,
                    MissionType.Transport,
                    100400,
                    new DateTime(2026, 5, 1),
                    100m,
                    "Client 1",
                    "0705123456",
                    "Str. Campului nr. 2",
                    "client1@gmail.com",
                    MissionStatus.Programata
                )
            };
            _mockRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(missions);

            // Act
            var updatedMissionId = await _service.UpdateMissionById(missionId, request);

            // Assert
            Assert.Equal(missionId, updatedMissionId);
            _mockRepo.Verify(repo => repo.GetAllMissions(null), Times.Once);
            Assert.Equal(newPhone, missions.First().Phone);
        }

        [Fact]
        public async Task UpdateMissionById_AddressHasValue_UpdatesMission()
        {
            // Arrange
            int missionId = 1;
            DateTime newDate = new DateTime(2026, 6, 1);
            var newAddress = "Str. Noua nr. 5";
            var request = new UpdateMissionRequest(null, null, null, null, null, null, newAddress, null, null);
            var missions = new List<Mission>
            {
                new(
                    missionId,
                    MissionType.Transport,
                    100400,
                    new DateTime(2026, 5, 1),
                    100m,
                    "Client 1",
                    "0705123456",
                    "Str. Campului nr. 2",
                    "client1@gmail.com",
                    MissionStatus.Programata
                )
            };
            _mockRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(missions);

            // Act
            var updatedMissionId = await _service.UpdateMissionById(missionId, request);

            // Assert
            Assert.Equal(missionId, updatedMissionId);
            _mockRepo.Verify(repo => repo.GetAllMissions(null), Times.Once);
            Assert.Equal(newAddress, missions.First().Address);
        }

        [Fact]
        public async Task UpdateMissionById_EmailHasValue_UpdatesMission()
        {
            // Arrange
            int missionId = 1;
            var newEmail = "newemail@gmail.com";
            var request = new UpdateMissionRequest(null, null, null, null, null, null, null, newEmail, null);
            var missions = new List<Mission>
            {
                new(
                    missionId,
                    MissionType.Transport,
                    100400,
                    new DateTime(2026, 5, 1),
                    100m,
                    "Client 1",
                    "0705123456",
                    "Str. Campului nr. 2",
                    "client1@gmail.com",
                    MissionStatus.Programata
                )
            };
            _mockRepo.Setup(repo => repo.GetAllMissions(null)).ReturnsAsync(missions);

            // Act
            var updatedMissionId = await _service.UpdateMissionById(missionId, request);

            // Assert
            Assert.Equal(missionId, updatedMissionId);
            _mockRepo.Verify(repo => repo.GetAllMissions(null), Times.Once);
            Assert.Equal(newEmail, missions.First().Email);
        }
    }
}