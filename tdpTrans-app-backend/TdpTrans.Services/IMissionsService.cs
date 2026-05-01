using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TdpTrans.DTOs;

namespace TdpTrans.Services
{
    public interface IMissionsService
    {
        Task<int> AddMission(CreateMissionRequest request);
        Task<IEnumerable<MissionResponse>> GetAllMissions();
        Task<PaginatedResult> GetMissionsPaginated(int page, int pageSize);
        Task<MissionResponse?> GetMissionById(int id);
        Task<int> UpdateMissionById(int id, UpdateMissionRequest request);
        Task DeleteMissionById(int id);
    }
}
