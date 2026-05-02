using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TdpTrans.DTOs;
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

        public Task<IEnumerable<Mission>> GetAllMissions(string? searchTerm = null)
        {
            var query = _missions.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(m =>
                    (m.Client != null && m.Client.ToLower().Contains(searchTerm)) ||
                    (m.Phone != null && m.Phone.Contains(searchTerm)) ||
                    m.Id.ToString().Contains(searchTerm)
                );
            }

            return Task.FromResult<IEnumerable<Mission>>(query.ToList());
        }

        public Task<Mission?> GetMissionById(int missionId)
        {
            return Task.FromResult(_missions.FirstOrDefault(mission => mission.Id == missionId));
        }
    }
}
