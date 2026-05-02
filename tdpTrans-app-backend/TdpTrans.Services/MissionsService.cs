using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TdpTrans.Repositories;
using TdpTrans.DTOs;
using TdpTrans.Models;

namespace TdpTrans.Services
{
    public class MissionsService : IMissionsService
    {
        private readonly IMissionsRepository _missionsRepository;
        private int _missionId = 1;

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
            return _missionId++;
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
            return new MissionStatisticsDTO
            {
                TotalComenzi = missions.Count(),
                TotalTransportMarfa = missions.Count(m => m.Type == MissionType.Transport),
                TotalTractari = missions.Count(m => m.Type == MissionType.Tractare)
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
