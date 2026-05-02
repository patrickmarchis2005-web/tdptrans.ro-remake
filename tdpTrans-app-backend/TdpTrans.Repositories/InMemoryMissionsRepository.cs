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
        private readonly List<Mission> _missions;

        public InMemoryMissionsRepository()
        {
            _missions = new List<Mission>();
            GenerateSampleData();
        }

        private void GenerateSampleData()
        {
            var sampleMissions = new List<Mission>
            {
                new(1, MissionType.Transport, 1, DateTime.Now.AddDays(-10), 1000, "John Doe", "1234567890", "Str. Primaverii nr. 2", "johndoe@gmail.com", MissionStatus.Finalizata),
                new(2, MissionType.Tractare, 2, DateTime.Now.AddDays(-5), 500, "Jane Smith", "0987654321", "Str. Libertatii nr. 5", "janesmith@email.com", MissionStatus.In_desfasurare),
                new(3, MissionType.Transport, 3, DateTime.Now.AddDays(2), 1500, "Bob Johnson", "0712345678", "Str. Independentei nr. 10", "bob@yahoo.com", MissionStatus.Programata),
                new(4, MissionType.Tractare, 4, DateTime.Now.AddDays(-1), 800, "Alice Brown", "0778147835", "Str. Victoriei nr. 8", "alicebrown@gmail.com", MissionStatus.In_desfasurare),
                new(5, MissionType.Transport, 5, DateTime.Now.AddDays(5), 1200, "Charlie Davis", "0798765432", "Str. Unirii nr. 3", "davis01@gmail.com", MissionStatus.Programata)
            };

            foreach (var mission in sampleMissions)
            {
                _missions.Add(mission);
            }
        }

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
