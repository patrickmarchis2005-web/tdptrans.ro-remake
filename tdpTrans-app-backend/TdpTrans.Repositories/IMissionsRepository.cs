using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TdpTrans.DTOs;
using TdpTrans.Models;

namespace TdpTrans.Repositories
{
    public interface IMissionsRepository
    {
        Task AddMission(Mission mission);
        Task<IEnumerable<Mission>> GetAllMissions(string? searchTerm = null);
        Task<Mission?> GetMissionById(int missionId);
        Task DeleteMission(Mission mission);
    }
}
