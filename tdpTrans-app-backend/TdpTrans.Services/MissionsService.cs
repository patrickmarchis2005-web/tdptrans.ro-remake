using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TdpTrans.DTOs;
using TdpTrans.Models;
using TdpTrans.Repositories;

namespace TdpTrans.Services
{
    public class MissionsService : IMissionsService
    {
        private readonly IMissionsRepository _missionsRepository;

        public MissionsService(IMissionsRepository missionsRepository)
        {
            _missionsRepository = missionsRepository;
        }


        public async Task<int> AddMission(CreateMissionRequest request)
        {
            if (!Enum.TryParse<MissionType>(request.MissionType, out var missionType))
            {
                throw new ArgumentException($"Invalid mission type: {request.MissionType}");
            }

            if (!Enum.TryParse<MissionStatus>(request.MissionStatus, out var missionStatus))
            {
                throw new ArgumentException($"Invalid mission status: {request.MissionStatus}");
            }

            var _missions = await _missionsRepository.GetAllMissions();
            var _missionId = _missions.Any() ? _missions.Max(m => m.Id) + 1 : 1;
            var mission = new Mission
            (
                _missionId,
                missionType,
                request.TruckId,
                request.Date,
                request.Cost,
                request.Client,
                request.Phone,
                request.Address,
                request.Email,
                missionStatus
            );

            await _missionsRepository.AddMission(mission);
            return _missionId;
        }

        public async Task DeleteMissionById(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException("Mission ID must be a positive integer.");
            }

            var missions = await _missionsRepository.GetAllMissions();
            var searchedMission = missions
                .Where(mission => mission.Id == id)
                .FirstOrDefault() ?? throw new InvalidOperationException($"Mission with ID {id} not found.");

            await _missionsRepository.DeleteMission(searchedMission);
        }

        public async Task<IEnumerable<MissionResponse>> GetAllMissions()
        {
            var missions = await _missionsRepository.GetAllMissions();
            return missions.Select(mission => Mapper.FromMissionToDTO(mission));
        }

        public async Task<MissionResponse?> GetMissionById(int id)
        {
            var missions = await _missionsRepository.GetAllMissions();
            return missions
                .Where(mission => mission.Id == id)
                .Select(mission => Mapper.FromMissionToDTO(mission))
                .FirstOrDefault();
        }

        public async Task<PaginatedResult> GetMissionsPaginated(int page, int pageSize, string? searchTerm = null)
        {
            if (page <= 0)
            {
                throw new ArgumentException("Page number must be a positive integer.");
            }
            if (pageSize <= 0)
            {
                throw new ArgumentException("Page size must be a positive integer.");
            }

            var missions = await _missionsRepository.GetAllMissions(searchTerm);
            var missionsResponse = missions.Select(mission => Mapper.FromMissionToDTO(mission));
            var totalCount = missionsResponse.Count();

            var paginatedResult = new PaginatedResult
            {
                Items = missionsResponse.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize
            };
            return paginatedResult;
        }

        public async Task<MissionStatisticsDTO> GetMissionStatistics()
        {
            var missions = await _missionsRepository.GetAllMissions();
            var groupedByMonth = missions
                .GroupBy(m => m.Date.ToString("MMM", CultureInfo.InvariantCulture))
                .Select(group => new MonthlyStatisticDTO
                {
                    Name = group.Key,
                    Towing = group.Count(m => m.Type == MissionType.Tractare),
                    Transport = group.Count(m => m.Type == MissionType.Transport)
                })
                .ToList();

            var monthOrder = new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "Unknown" };
            var sortedMonthlyData = groupedByMonth
                .OrderBy(m => monthOrder.IndexOf(m.Name))
                .ToList();

            return new MissionStatisticsDTO
            {
                TotalComenzi = missions.Count(),
                TotalTransportMarfa = missions.Count(m => m.Type == MissionType.Transport),
                TotalTractari = missions.Count(m => m.Type == MissionType.Tractare),
                MonthlyData = sortedMonthlyData
            };
        }

        public async Task<int> UpdateMissionById(int id, UpdateMissionRequest request)
        {
            var missions = await _missionsRepository.GetAllMissions();
            var searchedMission = missions
                .Where(mission => mission.Id == id)
                .FirstOrDefault() ?? throw new ArgumentException($"Mission with ID {id} not found.");

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
                searchedMission.TruckId = request.TruckId.Value;
            }
            if (request.Date.HasValue)
            {
                searchedMission.Date = request.Date.Value;
            }
            if (request.Cost.HasValue)
            {
                searchedMission.Cost = request.Cost.Value;
            }
            if (request.Client != null)
            {
                searchedMission.Client = request.Client;
            }
            if (request.Phone != null)
            {
                searchedMission.Phone = request.Phone;
            }
            if (request.Address != null)
            {
                searchedMission.Address = request.Address;
            }
            if (request.Email != null)
            {
                searchedMission.Email = request.Email;
            }

            return searchedMission.Id;
        }
    }
}
