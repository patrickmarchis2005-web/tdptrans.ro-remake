using TdpTrans.DTOs;

namespace TdpTrans.Services
{
    public interface IMissionsService
    {
        Task<int> AddMission(CreateMissionRequest request, int? actorUserId = null);
        Task<IEnumerable<MissionResponse>> GetAllMissions(int? actorUserId = null);
        Task<PaginatedResult> GetMissionsPaginated(int page, int pageSize, string? searchTerm = null, int? actorUserId = null);
        Task<MissionStatisticsDTO> GetMissionStatistics(int? actorUserId = null);
        Task<MissionResponse?> GetMissionById(int id, int? actorUserId = null);
        Task<int> UpdateMissionById(int id, UpdateMissionRequest request, int? actorUserId = null);
        Task DeleteMissionById(int id, int? actorUserId = null);
    }
}
