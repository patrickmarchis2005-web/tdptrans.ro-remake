using Microsoft.EntityFrameworkCore;
using TdpTrans.Models;
using TdpTrans.Repositories.Interfaces;

namespace TdpTrans.Repositories.Implementations
{
    public class MissionsRepository : IMissionsRepository
    {
        private readonly ApplicationDbContext _context;

        public MissionsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Mission> AddMission(Mission mission)
        {
            _context.Missions.Add(mission);
            await _context.SaveChangesAsync();
            return mission;
        }

        public async Task DeleteMission(Mission mission)
        {
            _context.Missions.Remove(mission);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Mission>> GetAllMissions(string? searchTerm = null)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await _context.Missions
                    .Include(mission => mission.Client)
                    .Include(mission => mission.Truck)
                    .ToListAsync();
            }

            return await _context.Missions
                .Include(mission => mission.Client)
                .Include(mission => mission.Truck)
                .Where(mission =>
                    mission.Client.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    mission.Client.Phone.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    mission.Id.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToListAsync();
        }

        public async Task<Mission?> GetMissionById(int missionId)
        {
            return await _context.Missions
                .Include(mission => mission.Client)
                .Include(mission => mission.Truck)
                .FirstOrDefaultAsync(mission => mission.Id == missionId);
        }

        public async Task SaveChanges()
        {
            await _context.SaveChangesAsync();
        }
    }
}
