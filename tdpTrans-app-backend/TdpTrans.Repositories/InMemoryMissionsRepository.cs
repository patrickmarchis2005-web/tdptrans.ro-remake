using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TdpTrans.Models;

namespace TdpTrans.Repositories
{
    public class InMemoryMissionsRepository : IMissionsRepository
    {
        private readonly List<Mission> _missions = new List<Mission>();

        public Task AddMission(Mission mission)
        {
            _missions.Add(mission);
            return Task.CompletedTask;
        }

        public Task DeleteMission(Mission mission)
        {
            _missions.Remove(mission);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Mission>> GetAllMissions()
        {
            return Task.FromResult<IEnumerable<Mission>>(_missions);
        }

        public Task<Mission?> GetMissionById(int missionId)
        {
            return Task.FromResult(_missions.FirstOrDefault(mission => mission.Id == missionId));
        }
    }
}
