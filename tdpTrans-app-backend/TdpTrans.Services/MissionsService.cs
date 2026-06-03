using System.Globalization;
using TdpTrans.DTOs;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;

namespace TdpTrans.Services
{
    public class MissionsService : IMissionsService
    {
        private readonly IMissionsRepository _missionsRepository;
        private readonly IClientsRepository _clientsRepository;
        private readonly ITrucksRepository _trucksRepository;
        private readonly IUserAccessService _userAccessService;
        private readonly IActivityLogService _activityLogService;

        public MissionsService(
            IMissionsRepository missionsRepository,
            IClientsRepository clientsRepository,
            ITrucksRepository trucksRepository,
            IUserAccessService userAccessService,
            IActivityLogService activityLogService)
        {
            _missionsRepository = missionsRepository;
            _clientsRepository = clientsRepository;
            _trucksRepository = trucksRepository;
            _userAccessService = userAccessService;
            _activityLogService = activityLogService;
        }

        public async Task<int> AddMission(CreateMissionRequest request, int? actorUserId = null)
        {
            await EnsureMissionPermission(actorUserId, "Tried to create a mission.");

            if (!Enum.TryParse<MissionType>(request.MissionType, out var missionType))
            {
                throw new ArgumentException($"Invalid mission type: {request.MissionType}");
            }

            if (!Enum.TryParse<MissionStatus>(request.MissionStatus, out var missionStatus))
            {
                throw new ArgumentException($"Invalid mission status: {request.MissionStatus}");
            }

            var client = await _clientsRepository.GetClientByEmail(request.Email);
            if (client == null)
            {
                client = new Client
                {
                    Name = request.Client,
                    Phone = request.Phone,
                    Email = request.Email
                };
                await _clientsRepository.AddClient(client);
            }

            var truck = await _trucksRepository.GetTruckById(request.TruckId);
            if (truck == null)
            {
                throw new ArgumentException($"Truck with ID {request.TruckId} not found in the database.");
            }

            var mission = new Mission
            {
                Type = missionType,
                Status = missionStatus,
                Date = NormalizeMissionDate(request.Date),
                Cost = request.Cost,
                Address = request.Address,
                ClientId = client.Id,
                TruckId = truck.Id
            };

            var createdMission = await _missionsRepository.AddMission(mission);

            if (actorUserId.HasValue)
            {
                await _activityLogService.Log(actorUserId, ActivityActionNames.MissionCreated, $"Created mission #{createdMission.Id} for {client.Name}.");
            }

            return createdMission.Id;
        }

        public async Task DeleteMissionById(int id, int? actorUserId = null)
        {
            await EnsureMissionPermission(actorUserId, "Tried to delete a mission.");

            if (id <= 0)
            {
                throw new ArgumentException("Mission ID must be a positive integer.");
            }

            var searchedMission = await _missionsRepository.GetMissionById(id)
                ?? throw new InvalidOperationException($"Mission with ID {id} not found.");

            await _missionsRepository.DeleteMission(searchedMission);

            if (actorUserId.HasValue)
            {
                await _activityLogService.Log(actorUserId, ActivityActionNames.MissionDeleted, $"Deleted mission #{id}.");
            }
        }

        public async Task<IEnumerable<MissionResponse>> GetAllMissions(int? actorUserId = null)
        {
            await EnsureMissionPermission(actorUserId, "Tried to view all missions.");

            var missions = await _missionsRepository.GetAllMissions();
            var missionDtos = missions.Select(Mapper.FromMissionToDTO).ToList();

            if (actorUserId.HasValue)
            {
                await _activityLogService.Log(actorUserId, ActivityActionNames.MissionsViewed, "Viewed the complete missions list.");
            }

            return missionDtos;
        }

        public async Task<MissionResponse?> GetMissionById(int id, int? actorUserId = null)
        {
            await EnsureMissionPermission(actorUserId, "Tried to view a mission by id.");

            var mission = await _missionsRepository.GetMissionById(id);
            if (mission == null)
            {
                return null;
            }

            if (actorUserId.HasValue)
            {
                await _activityLogService.Log(actorUserId, ActivityActionNames.MissionsViewed, $"Viewed mission #{id}.");
            }

            return Mapper.FromMissionToDTO(mission);
        }

        public async Task<PaginatedResult> GetMissionsPaginated(int page, int pageSize, string? searchTerm = null, int? actorUserId = null)
        {
            await EnsureMissionPermission(actorUserId, "Tried to view paginated missions.");

            if (page <= 0)
            {
                throw new ArgumentException("Page number must be a positive integer.");
            }

            if (pageSize <= 0)
            {
                throw new ArgumentException("Page size must be a positive integer.");
            }

            var missions = await _missionsRepository.GetAllMissions(searchTerm);
            var missionsResponse = missions.Select(Mapper.FromMissionToDTO).ToList();

            if (actorUserId.HasValue)
            {
                await _activityLogService.Log(
                    actorUserId,
                    ActivityActionNames.MissionsViewed,
                    $"Viewed paginated missions page {page} with search '{searchTerm ?? string.Empty}'.");
            }

            return new PaginatedResult
            {
                Items = missionsResponse.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                TotalCount = missionsResponse.Count,
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<MissionStatisticsDTO> GetMissionStatistics(int? actorUserId = null)
        {
            await EnsureMissionPermission(actorUserId, "Tried to view mission statistics.");

            var missions = await _missionsRepository.GetAllMissions();
            var groupedByMonth = missions
                .GroupBy(mission => mission.Date.ToString("MMM", CultureInfo.InvariantCulture))
                .Select(group => new MonthlyStatisticDTO
                {
                    Name = group.Key,
                    Towing = group.Count(mission => mission.Type == MissionType.Tractare),
                    Transport = group.Count(mission => mission.Type == MissionType.Transport)
                })
                .ToList();

            var monthOrder = new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "Unknown" };
            var sortedMonthlyData = groupedByMonth
                .OrderBy(month => monthOrder.IndexOf(month.Name))
                .ToList();

            if (actorUserId.HasValue)
            {
                await _activityLogService.Log(actorUserId, ActivityActionNames.StatisticsViewed, "Viewed mission statistics.");
            }

            return new MissionStatisticsDTO
            {
                TotalComenzi = missions.Count(),
                TotalTransportMarfa = missions.Count(mission => mission.Type == MissionType.Transport),
                TotalTractari = missions.Count(mission => mission.Type == MissionType.Tractare),
                MonthlyData = sortedMonthlyData
            };
        }

        public async Task<int> UpdateMissionById(int id, UpdateMissionRequest request, int? actorUserId = null)
        {
            await EnsureMissionPermission(actorUserId, "Tried to update a mission.");

            var searchedMission = await _missionsRepository.GetMissionById(id)
                ?? throw new ArgumentException($"Mission with ID {id} not found.");

            if (request.MissionType != null)
            {
                if (!Enum.TryParse<MissionType>(request.MissionType, out var missionType))
                {
                    throw new ArgumentException($"Invalid mission type: {request.MissionType}");
                }

                searchedMission.Type = missionType;
            }

            if (request.MissionStatus != null)
            {
                if (!Enum.TryParse<MissionStatus>(request.MissionStatus, out var missionStatus))
                {
                    throw new ArgumentException($"Invalid mission status: {request.MissionStatus}");
                }

                searchedMission.Status = missionStatus;
            }

            if (request.TruckId.HasValue)
            {
                var truck = await _trucksRepository.GetTruckById(request.TruckId.Value);
                if (truck == null)
                {
                    throw new ArgumentException($"Truck with ID {request.TruckId.Value} not found in the database.");
                }

                searchedMission.TruckId = request.TruckId.Value;
            }

            if (request.Date.HasValue)
            {
                searchedMission.Date = NormalizeMissionDate(request.Date.Value);
            }

            if (request.Cost.HasValue)
            {
                searchedMission.Cost = request.Cost.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.Client))
            {
                searchedMission.Client.Name = request.Client;
            }

            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                searchedMission.Client.Phone = request.Phone;
            }

            if (!string.IsNullOrWhiteSpace(request.Address))
            {
                searchedMission.Address = request.Address;
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                searchedMission.Client.Email = request.Email;
            }

            await _missionsRepository.SaveChanges();

            if (actorUserId.HasValue)
            {
                await _activityLogService.Log(actorUserId, ActivityActionNames.MissionUpdated, $"Updated mission #{id}.");
            }

            return searchedMission.Id;
        }

        private async Task EnsureMissionPermission(int? actorUserId, string actionDescription)
        {
            if (actorUserId.HasValue)
            {
                await _userAccessService.EnsurePermission(actorUserId.Value, PermissionNames.MissionsManage, actionDescription);
            }
        }

        private static DateTime NormalizeMissionDate(DateTime date)
        {
            return date.Kind switch
            {
                DateTimeKind.Utc => date,
                DateTimeKind.Local => date.ToUniversalTime(),
                _ => DateTime.SpecifyKind(date, DateTimeKind.Utc),
            };
        }
    }
}
